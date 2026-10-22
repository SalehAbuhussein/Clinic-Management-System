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

        /// <summary>
        /// The WHERE clause the list filters by, with its parameters added to
        /// the command. The list and GetTotalCount share it, so a filter added
        /// to one applies to the other and the two cannot disagree.
        /// </summary>
        /// <param name="command">The command the parameters go on.</param>
        /// <returns>The clause to append, or an empty string when nothing is filtered.</returns>
        private static string _VisitFilters(SqlCommand command, int? doctorId, int? patientId, DateTimeOffset? from, DateTimeOffset? to)
        {
            List<string> conditions = new List<string>();

            if (doctorId.HasValue)
            {
                conditions.Add("A.DoctorID = @DoctorID");
                command.Parameters.AddWithValue("@DoctorID", doctorId.Value);
            }

            if (patientId.HasValue)
            {
                conditions.Add("A.PatientID = @PatientID");
                command.Parameters.AddWithValue("@PatientID", patientId.Value);
            }

            if (from.HasValue)
            {
                conditions.Add("V.CheckinTime >= @From");
                command.Parameters.AddWithValue("@From", from.Value);
            }

            if (to.HasValue)
            {
                conditions.Add("V.CheckinTime < @To");
                command.Parameters.AddWithValue("@To", to.Value);
            }

            return conditions.Count == 0 ? string.Empty : $"\nWHERE {string.Join(" AND ", conditions)}";
        }

        /// <summary>
        /// How many rows match the filters, ignoring paging.
        ///
        /// A page past the last one returns nothing, so the list query's
        /// COUNT(*) OVER() has no row to ride on. This answers it directly.
        /// </summary>
        /// <returns>The number of matching rows.</returns>
        public static async Task<int> GetTotalCount(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    string query = @"SELECT COUNT(*)
                    FROM Visits V
                    INNER JOIN Appointments A ON A.AppointmentID = V.AppointmentID";

                    command.CommandText = query + _VisitFilters(command, doctorId, patientId, from, to);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public static async Task<DataTable> FindAllAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int pageNumber = 1, int pageSize = 10)
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

                    query += _VisitFilters(command, doctorId, patientId, from, to);

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

        public static async Task<DataRow?> FindAsync(int? id)
        {
            if (!id.HasValue)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    V.VisitID, V.AppointmentID, V.CheckinTime, V.CheckoutTime, V.Notes, V.IsLocked,
                    A.StartAt, A.PatientID, A.DoctorID,

                    PT.PersonID,
                    PP.Firstname AS PatientFirstname,
                    PP.Midname   AS PatientMidname,
                    PP.Thirdname AS PatientThirdname,
                    PP.Lastname  AS PatientLastname,

                    D.UserID,
                    DP.Firstname AS DoctorFirstname,
                    DP.Midname   AS DoctorMidname,
                    DP.Thirdname AS DoctorThirdname,
                    DP.Lastname  AS DoctorLastname
                FROM Visits V
                INNER JOIN Appointments A ON A.AppointmentID = V.AppointmentID
                INNER JOIN Patients PT ON PT.PatientID = A.PatientID
                INNER JOIN People PP ON PP.PersonID = PT.PersonID
                INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                INNER JOIN Users U ON U.UserID = D.UserID
                INNER JOIN People DP ON DP.PersonID = U.PersonID
                WHERE V.VisitID = @VisitID";

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

                string query = @"SELECT
                    V.VisitID, V.AppointmentID, V.CheckinTime, V.CheckoutTime, V.Notes, V.IsLocked,

                    A.StartAt, A.PatientID, A.DoctorID,

                    PT.PersonID,
                    PP.Firstname AS PatientFirstname,
                    PP.Midname   AS PatientMidname,
                    PP.Thirdname AS PatientThirdname,
                    PP.Lastname  AS PatientLastname,

                    D.UserID,
                    DP.Firstname AS DoctorFirstname,
                    DP.Midname   AS DoctorMidname,
                    DP.Thirdname AS DoctorThirdname,
                    DP.Lastname  AS DoctorLastname
                FROM Visits V
                INNER JOIN Appointments A ON A.AppointmentID = V.AppointmentID
                INNER JOIN Patients PT ON PT.PatientID = A.PatientID
                INNER JOIN People PP ON PP.PersonID = PT.PersonID
                INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                INNER JOIN Users U ON U.UserID = D.UserID
                INNER JOIN People DP ON DP.PersonID = U.PersonID
                WHERE V.AppointmentID = @AppointmentID";

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
