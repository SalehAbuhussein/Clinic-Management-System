using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsVisitDA
    {
        public static async Task<DataTable> FindAllAsync(int? doctorId = null, DateOnly? day = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,

                    V.VisitID, V.AppointmentID, V.CheckinTime, V.CheckoutTime, V.Notes, V.IsLocked,

                    A.StartAt,

                    CONCAT_WS(' ', PP.Firstname, PP.Midname, PP.Thirdname, PP.Lastname) AS PatientName,
                    CONCAT_WS(' ', DP.Firstname, DP.Midname, DP.Thirdname, DP.Lastname) AS DoctorName

                FROM Visits V
                INNER JOIN Appointments A ON A.AppointmentID = V.AppointmentID
                INNER JOIN Patients PT ON PT.PatientID = A.PatientID
                INNER JOIN People PP ON PP.PersonID = PT.PersonID
                INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                INNER JOIN Users U ON U.UserID = D.UserID
                INNER JOIN People DP ON DP.PersonID = U.PersonID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    List<string> conditions = new List<string>();

                    if (doctorId.HasValue)
                    {
                        conditions.Add("A.DoctorID = @DoctorID");
                        command.Parameters.AddWithValue("@DoctorID", doctorId.Value);
                    }

                    if (day.HasValue)
                    {
                        conditions.Add("V.CheckinTime >= @Day AND V.CheckinTime < DATEADD(day, 1, @Day)");
                        command.Parameters.AddWithValue("@Day", day.Value);
                    }

                    if (conditions.Count > 0)
                        query += $"\nWHERE {string.Join(" AND ", conditions)}";

                    query += @"
                        ORDER BY V.CheckinTime DESC
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

        /// <summary>Returns the row; ref parameters cannot cross an await (CS1988).</summary>
        public static async Task<DataRow?> FindAsync(int? id)
        {
            if (!id.HasValue)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT [VisitID]
                    ,[AppointmentID]
                    ,[CheckinTime]
                    ,[CheckoutTime]
                    ,[Notes]
                    ,[isLocked]
                FROM [dbo].[Visits]
                WHERE VisitID = @VisitID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static async Task<DataRow?> FindByAppointmentIDAsync(int? id)
        {
            if (!id.HasValue)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT [VisitID]
                    ,[AppointmentID]
                    ,[CheckinTime]
                    ,[CheckoutTime]
                    ,[Notes]
                    ,[IsLocked]
                FROM [dbo].[Visits]
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static async Task<bool> IsExistByAppointmentIDAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1 FROM [dbo].[Visits] WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static async Task<bool> HasCheckedoutAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool hasCheckedout = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1 FROM Visits WHERE VisitID = @VisitID AND IsLocked = 1 AND CheckoutTime IS NOT NULL";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        hasCheckedout = reader.HasRows;
                    }
                }
            }

            return hasCheckedout;
        }

        public static async Task<int?> CreateAsync(
            int appointmentID,
            DateTimeOffset checkinTime,
            DateTimeOffset? checkoutTime,
            string? notes
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
                {
                    string query = @"INSERT INTO [dbo].[Visits]
                        ([AppointmentID]
                        ,[CheckinTime]
                        ,[CheckoutTime]
                        ,[Notes])
                    VALUES
                        (@AppointmentID
                        ,@CheckinTime
                        ,@CheckoutTime
                        ,@Notes);

                    DECLARE @VisitID INT = SCOPE_IDENTITY();

                    UPDATE [dbo].[Appointments]
                    SET [Status] = 1
                    WHERE AppointmentID = @AppointmentID;

                    SELECT @VisitID;";

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@AppointmentID", appointmentID);
                        command.Parameters.AddWithValue("@CheckinTime", checkinTime);
                        command.Parameters.AddWithValue("@CheckoutTime", checkoutTime == null ? DBNull.Value : checkoutTime);
                        command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);

                        object? result = await command.ExecuteScalarAsync();

                        if (result != null && int.TryParse(result.ToString(), out int insertedId))
                        {
                            id = insertedId;
                        }
                    }

                    await transaction.CommitAsync();
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(
            int? id,
            int appointmentID,
            DateTimeOffset checkinTime,
            DateTimeOffset? checkoutTime,
            string? notes,
            bool isLocked
        )
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Visits]
                SET [AppointmentID] = @AppointmentID
                    ,[CheckinTime] = @CheckinTime
                    ,[CheckoutTime] = @CheckoutTime
                    ,[Notes] = @Notes
                    ,[IsLocked] = @IsLocked
                WHERE VisitID = @VisitID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", id);
                    command.Parameters.AddWithValue("@AppointmentID", appointmentID);
                    command.Parameters.AddWithValue("@CheckinTime", checkinTime);
                    command.Parameters.AddWithValue("@CheckoutTime", checkoutTime == null ? DBNull.Value : checkoutTime);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
                    command.Parameters.AddWithValue("@IsLocked", isLocked);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> DeleteAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool hasDeleted = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("DeleteVisit", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@VisitID", id);

                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    command.Parameters.Add(returnValue);

                    await command.ExecuteNonQueryAsync();

                    int result = (int)returnValue.Value;

                    hasDeleted = result == 1;
                }
            }

            return hasDeleted;
        }
    }
}
