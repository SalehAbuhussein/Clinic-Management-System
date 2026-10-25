using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsLeaveDA
    {
        private static string _LeaveFilters(SqlCommand command, int? userId, DateTimeOffset? from, DateTimeOffset? to, byte? status)
        {
            List<string> conditions = new List<string>();

            if (userId.HasValue)
            {
                conditions.Add("U.UserID = @UserID");
                command.Parameters.AddWithValue("@UserID", userId);
            }

            if (from.HasValue)
            {
                conditions.Add("L.EndAt >= @From");
                command.Parameters.AddWithValue("@From", from.Value);
            }

            if (to.HasValue)
            {
                conditions.Add("L.StartAt < @To");
                command.Parameters.AddWithValue("@To", to.Value);
            }

            if (status.HasValue)
            {
                conditions.Add("L.Status = @Status");
                command.Parameters.AddWithValue("@Status", status.Value);
            }

            return conditions.Count == 0 ? string.Empty : $"\nWHERE {string.Join(" AND ", conditions)}";
        }

        public static async Task<int> GetTotalCountAsync(int? userId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, byte? status = null, CancellationToken cancellationToken = default)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    string query = @"SELECT COUNT(*)
                    FROM Leaves L
                    INNER JOIN Users U ON U.UserID = L.UserID
                    INNER JOIN People P ON U.PersonID = P.PersonID";

                    command.CommandText = query + _LeaveFilters(command, userId, from, to, status);

                    return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
                }
            }
        }

        public static async Task<DataTable> FindAllAsync(
            int? userId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            byte? status = null,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default
        )
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);
                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,
                    *,
                    CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname) AS Fullname
                FROM Leaves L
                INNER JOIN Users U ON U.UserID = L.UserID
                INNER JOIN People P ON U.PersonID = P.PersonID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    query += _LeaveFilters(command, userId, from, to, status);

                    query += @"
                        ORDER BY L.LeaveID
                        OFFSET (@PageNumber - 1) * @PageSize ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";

                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    command.CommandText = query;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (reader.HasRows)
                        {
                            dt.Load(reader);
                        }
                    }
                }
            }

            return dt;
        }

        public static async Task<DataRow?> FindAsync(int id)
        {
            if (id < 1)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,
                    *
                FROM Leaves L
                INNER JOIN Users U ON U.UserID = L.UserID
                INNER JOIN People P ON U.PersonID = P.PersonID
                WHERE L.LeaveID = @LeaveID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LeaveID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static async Task<int?> CreateAsync(
            int? userId,
            DateTimeOffset from,
            DateTimeOffset to,
            string reason
        )
        {
            int? id = null;

            if (!userId.HasValue)
                return null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO [dbo].[Leaves]
                    ([UserID]
                    ,[StartAt]
                    ,[EndAt]
                    ,[Reason])
                VALUES
                    (@UserID
                    ,@StartAt
                    ,@EndAt
                    ,@Reason); SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@StartAt", from);
                    command.Parameters.AddWithValue("@EndAt", to);
                    command.Parameters.AddWithValue("@Reason", reason);

                    object? result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(int? leaveId, string reason, byte status)
        {
            if (!leaveId.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Leaves]
	                SET [Reason] = @Reason,
                    Status = @Status
                WHERE LeaveID = @LeaveID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LeaveID", leaveId);
                    command.Parameters.AddWithValue("@Reason", reason);
                    command.Parameters.AddWithValue("@Status", status);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> DeleteAsync(int? leaveId)
        {
            if (!leaveId.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"DELETE FROM Leaves WHERE LeaveID = @LeaveID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LeaveID", leaveId);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> IsExistByIdAsync(int id)
        {
            if (id < 1)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT TOP 1 1 FROM Leaves WHERE LeaveID = @LeaveID;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LeaveID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static async Task<bool> HasDoctorLeaveAsync(int? doctorId, DateTimeOffset startAt, DateTimeOffset endAt)
        {
            if (!doctorId.HasValue)
                return false;

            bool hasLeave = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT TOP 1 true=1 FROM Leaves L
                    INNER JOIN Doctors D ON L.UserID = D.UserID
                    WHERE StartAt < @EndAt AND EndAt > @StartAt AND D.DoctorID = @DoctorID AND L.Status != 2";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DoctorID", doctorId);
                    command.Parameters.AddWithValue("@StartAt", startAt);
                    command.Parameters.AddWithValue("@EndAt", endAt);

                    object? result = await command.ExecuteScalarAsync();

                    hasLeave = result != null;
                }
            }

            return hasLeave;
        }

        public static async Task<bool> HasUserLeaveAsync(int? userId, DateTimeOffset startAt, DateTimeOffset endAt)
        {
            if (!userId.HasValue)
                return false;

            bool hasLeave = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT TOP 1 true=1 FROM Leaves L
                    WHERE L.UserID = @UserID
                        AND L.StartAt < @EndAt
                        AND L.EndAt > @StartAt
                        AND L.Status != 2";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@StartAt", startAt);
                    command.Parameters.AddWithValue("@EndAt", endAt);

                    object? result = await command.ExecuteScalarAsync();

                    hasLeave = result != null;
                }
            }

            return hasLeave;
        }
    }
}
