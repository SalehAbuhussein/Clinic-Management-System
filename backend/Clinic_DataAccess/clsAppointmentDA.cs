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
    public class clsAppointmentDA
    {
        /// <summary>
        /// The WHERE clause the appointment list filters by, with its
        /// parameters added to the command. The list and GetTotalCount share
        /// it, so a filter added to one applies to the other.
        ///
        /// Every filter is qualified with A: DoctorID and PatientID each exist
        /// on two of the joined tables, so an unqualified name is ambiguous SQL.
        /// </summary>
        /// <param name="command">The command the parameters go on.</param>
        /// <returns>The clause to append, or an empty string when nothing is filtered.</returns>
        private static string _AppointmentFilters(SqlCommand command, int? doctorId, int? patientId, DateTimeOffset? from, DateTimeOffset? to)
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
                conditions.Add("A.EndAt >= @From");
                command.Parameters.AddWithValue("@From", from.Value);
            }

            if (to.HasValue)
            {
                conditions.Add("A.StartAt < @To");
                command.Parameters.AddWithValue("@To", to.Value);
            }

            return conditions.Count == 0 ? string.Empty : $"\nWHERE {string.Join(" AND ", conditions)}";
        }

        /// <summary>
        /// How many appointments match the filters, ignoring paging.
        ///
        /// A page past the last one returns nothing, so the list query's
        /// COUNT(*) OVER() has no row to ride on. This answers it directly.
        /// </summary>
        /// <returns>The number of matching appointments.</returns>
        public static async Task<int> GetTotalCount(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    string query = @"SELECT COUNT(*) FROM Appointments A";

                    command.CommandText = query + _AppointmentFilters(command, doctorId, patientId, from, to);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        /// <summary>
        /// How many live appointments a doctor has in a range, ignoring paging.
        ///
        /// The WHERE repeats FindDoctorSchedules rather than sharing a helper,
        /// because that query filters on fixed columns with no optional parts.
        /// </summary>
        /// <returns>The number of matching slots.</returns>
        public static async Task<int> GetDoctorSchedulesTotalCount(int doctorId, DateTimeOffset from, DateTimeOffset to)
        {
            if (doctorId < 1)
                return 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT COUNT(*)
                FROM Appointments A
                WHERE A.DoctorID = @DoctorID
                AND A.StartAt >= @StartAt
                AND A.EndAt < @EndAt
                AND A.Status NOT IN (1, 2, 3)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DoctorID", doctorId);
                    command.Parameters.AddWithValue("@StartAt", from);
                    command.Parameters.AddWithValue("@EndAt", to);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public static async Task<DataTable> FindAllAsync(
            int? doctorId,
            int? patientId,
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
                    A.AppointmentID, A.DoctorID, A.PatientID, A.StartAt, A.EndAt,
                    A.Status, A.Notes, A.CreatedAt, A.UpdatedAt, A.CreatedBy,

                    P.PersonID,

                    COUNT(*) OVER() AS TotalCount,

                    PP.Firstname AS PatientFirstname,
                    PP.Midname   AS PatientMidname,
                    PP.Thirdname AS PatientThirdname,
                    PP.Lastname  AS PatientLastname,

                    D.UserID,
                    DP.Firstname AS DoctorFirstname,
                    DP.Midname   AS DoctorMidname,
                    DP.Thirdname AS DoctorThirdname,
                    DP.Lastname  AS DoctorLastname,
                    V.VisitID
                    FROM Appointments A
                    INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                    INNER JOIN Users U ON U.UserID = D.UserID
                    INNER JOIN People DP ON DP.PersonID = U.PersonID
                    INNER JOIN Patients P ON P.PatientID = A.PatientID
                    INNER JOIN People PP ON PP.PersonID = P.PersonID
                    LEFT JOIN Visits V ON V.AppointmentID = A.AppointmentID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    query += _AppointmentFilters(command, doctorId, patientId, from, to);

                    query += @"
                        ORDER BY A.AppointmentID
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

        /// <summary>
        /// Returns the row instead of filling ten ref parameters. ref cannot cross an await
        /// (CS1988), and the caller no longer has to pre-declare ten locals seeded with
        /// MinValue just to receive them.
        /// </summary>
        public static async Task<DataRow?> FindAsync(int id)
        {
            if (id < 1)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    A.AppointmentID, A.DoctorID, A.PatientID, A.StartAt, A.EndAt,
                    A.Status, A.Notes, A.CreatedAt, A.UpdatedAt, A.CreatedBy,

                    P.PersonID,
                    PP.Firstname AS PatientFirstname,
                    PP.Midname   AS PatientMidname,
                    PP.Thirdname AS PatientThirdname,
                    PP.Lastname  AS PatientLastname,

                    D.UserID,
                    DP.Firstname AS DoctorFirstname,
                    DP.Midname   AS DoctorMidname,
                    DP.Thirdname AS DoctorThirdname,
                    DP.Lastname  AS DoctorLastname,
                    V.VisitID
                    FROM Appointments A
                    INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                    INNER JOIN Users U ON U.UserID = D.UserID
                    INNER JOIN People DP ON DP.PersonID = U.PersonID
                    INNER JOIN Patients P ON P.PatientID = A.PatientID
                    INNER JOIN People PP ON PP.PersonID = P.PersonID
                    LEFT JOIN Visits V ON V.AppointmentID = A.AppointmentID
                    WHERE A.AppointmentID = @AppointmentID";

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

        public static async Task<DataTable> FindDoctorSchedules(int doctorId, DateTimeOffset from, DateTimeOffset to, int pageNumber, int pageSize)
        {
            DataTable dt = new DataTable();

            if (doctorId < 1)
                return dt;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    A.AppointmentID, A.DoctorID, A.PatientID, A.StartAt, A.EndAt, A.Status,

                    COUNT(*) OVER() AS TotalCount
                FROM Appointments A
                WHERE A.DoctorID = @DoctorID
                AND A.StartAt >= @StartAt
                AND A.EndAt < @EndAt
                AND A.Status NOT IN (1, 2, 3)
                ORDER BY A.StartAt, A.AppointmentID
                OFFSET (@PageNumber - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.Parameters.AddWithValue("@DoctorID", doctorId);
                    command.Parameters.AddWithValue("@StartAt", from);
                    command.Parameters.AddWithValue("@EndAt", to);
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    command.CommandText = query;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt;
        }

        public static async Task<bool> IsExistByIDAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID";

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

        public static async Task<bool> IsAppointmentScheduledAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID AND Status = 0";

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

        public static async Task<bool> IsAppointmentAttendedAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID AND Status = 1";

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

        public static async Task<bool> IsAppointmentCancelledAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID AND Status = 3";

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

        public static async Task<int?> CreateAsync(
            int? doctorID,
            int patientID,
            DateTimeOffset startAt,
            DateTimeOffset endAt,
            byte status,
            string? notes,
            int createdBy
        )
        {
            if (!doctorID.HasValue)
                return null;

            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO [dbo].[Appointments]
                    ([DoctorID]
                    ,[PatientID]
                    ,[Status]
                    ,[Notes]
                    ,[CreatedBy]
                    ,[StartAt]
                    ,[EndAt])
                VALUES
                    (@DoctorID
                    ,@PatientID
                    ,@Status
                    ,@Notes
                    ,@CreatedBy
                    ,@StartAt
                    ,@EndAt); SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DoctorID", doctorID);
                    command.Parameters.AddWithValue("@PatientID", patientID);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
                    command.Parameters.AddWithValue("@StartAt", startAt);
                    command.Parameters.AddWithValue("@EndAt", endAt);
                    command.Parameters.AddWithValue("@CreatedBy", createdBy);

                    object? result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(
            int? id,
            int? doctorID,
            int patientID,
            DateTimeOffset startAt,
            DateTimeOffset endAt,
            byte status,
            string? notes
        )
        {
            if (!id.HasValue ||!doctorID.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Appointments]
                SET [DoctorID] = @DoctorID
                    ,[PatientID] = @PatientID
                    ,[Status] = @Status
                    ,[Notes] = @Notes
                    ,[StartAt] = @StartAt
                    ,[EndAt] = @EndAt
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);
                    command.Parameters.AddWithValue("@DoctorID", doctorID);
                    command.Parameters.AddWithValue("@PatientID", patientID);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
                    command.Parameters.AddWithValue("@StartAt", startAt);
                    command.Parameters.AddWithValue("@EndAt", endAt);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> DeleteAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"DELETE FROM [dbo].[Appointments] WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> HasDoctorExistingAppointmentAsync(int? doctorId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null)
        {
            if (!doctorId.HasValue)
                return false;

            bool hasOverlap = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1
                FROM Appointments A
                WHERE A.DoctorID = @DoctorID
                AND A.StartAt < @EndAt
                AND A.EndAt > @StartAt
                AND A.Status NOT IN  (1, 2, 3)
                AND (@ExcludeAppointmentID IS NULL OR A.AppointmentID != @ExcludeAppointmentID);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DoctorID", doctorId);
                    command.Parameters.AddWithValue("@StartAt", startAt);
                    command.Parameters.AddWithValue("@EndAt", endAt);
                    command.Parameters.AddWithValue("@ExcludeAppointmentID", (object?)excludeAppointmentID ?? DBNull.Value);

                    object? result = await command.ExecuteScalarAsync();

                    hasOverlap = result != null;
                }
            }

            return hasOverlap;
        }

        public static async Task<bool> HasPatientExistingAppointmentAsync(int patientId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null)
        {
            bool hasOverlap = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1
                FROM Appointments A
                WHERE A.PatientID = @PatientID
                AND A.StartAt < @EndAt
                AND A.EndAt > @StartAt
                AND A.Status NOT IN  (1, 2, 3)
                AND (@ExcludeAppointmentID IS NULL OR A.AppointmentID != @ExcludeAppointmentID);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PatientID", patientId);
                    command.Parameters.AddWithValue("@StartAt", startAt);
                    command.Parameters.AddWithValue("@EndAt", endAt);
                    command.Parameters.AddWithValue("@ExcludeAppointmentID", (object?)excludeAppointmentID ?? DBNull.Value);

                    object? result = await command.ExecuteScalarAsync();

                    hasOverlap = result != null;
                }
            }

            return hasOverlap;
        }

        public static async Task<bool> AttendAppointmentAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Appointments]
                SET [Status] = 1
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> UnattendAppointmentAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Appointments]
                SET [Status] = 2
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> CancelAppointmentAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Appointments]
                SET [Status] = 3
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }
    }
}
