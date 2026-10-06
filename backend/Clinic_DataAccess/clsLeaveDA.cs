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
        public static async Task<DataTable> FindAllAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10
        )
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,
                    *
                FROM Leaves L
                INNER JOIN Users U ON U.UserID = L.UserID
                INNER JOIN People P ON U.PersonID = P.PersonID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    List<string> conditions = new List<string>();

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

                    if (conditions.Count > 0)
                        query += $"\nWHERE {string.Join(" AND ", conditions)}";

                    query += @"
                        ORDER BY L.LeaveID
                        OFFSET (@PageNumber - 1) * @PageSize ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";

                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    command.CommandText = query;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
            DateTimeOffset? from,
            DateTimeOffset? to,
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
                    ,[Reason]
                    ,[Status])
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

        public static async Task<bool> UpdateAsync(int? leaveId, string reason)
        {
            if (!leaveId.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Leaves]
	                SET [Reason] = @Reason
                WHERE LeaveID = @LeaveID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LeaveID", leaveId);

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

                string query = @"SELECT true=1 FROM Leaves WHERE LeaveID = @LeaveID;";

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
    }
}
