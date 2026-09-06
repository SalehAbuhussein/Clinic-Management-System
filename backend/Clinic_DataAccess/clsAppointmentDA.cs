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
        public static DataTable FindAll(
            int? doctorId,
            int? patientId,
            DateTime? date,
            int pageNumber = 1,
            int pageSize = 10
        )
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT *
                FROM Appointments";

                List<string> conditions = new List<string>();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    if (doctorId.HasValue)
                    {
                        conditions.Add("DoctorID = @DoctorID");
                        command.Parameters.AddWithValue("@DoctorID", doctorId.Value);
                    }

                    if (patientId.HasValue)
                    {
                        conditions.Add("PatientID = @PatientID");
                        command.Parameters.AddWithValue("@PatientID", patientId.Value);
                    }

                    if (date.HasValue)
                    {
                        conditions.Add("StartAt < DATEADD(day, 1, @Date) AND EndAt >= @Date");
                        command.Parameters.AddWithValue("@Date", date.Value);
                    }

                    if (conditions.Count > 0)
                        query += $"\nWHERE {string.Join(" AND ", conditions)}";

                    query += @"
                        ORDER BY AppointmentID
                        OFFSET (@PageNumber - 1) * @PageSize ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";

                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    command.CommandText = query;

                    using (SqlDataReader reader = command.ExecuteReader())
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

        public static bool Find(
            int id,
            ref int doctorID,
            ref int patientID,
            ref DateTime startAt,
            ref DateTime endAt,
            ref byte status,
            ref string? notes,
            ref DateTime createdAt,
            ref DateTime updatedAt,
            ref int createdBy
        )
        {
            if (id == int.MinValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT [AppointmentID]
                    ,[DoctorID]
                    ,[PatientID]
                    ,[StartAt]
                    ,[EndAt]
                    ,[Status]
                    ,[Notes]
                    ,[CreatedAt]
                    ,[UpdatedAt]
                    ,[CreatedBy]
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            doctorID = (int)reader["DoctorID"];
                            patientID = (int)reader["PatientID"];
                            startAt = (DateTime)reader["StartAt"];
                            endAt = (DateTime)reader["EndAt"];
                            status = (byte)reader["Status"];
                            notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString();
                            createdAt = (DateTime)reader["CreatedAt"];
                            updatedAt = (DateTime)reader["UpdatedAt"];
                            createdBy = (int)reader["CreatedBy"];
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool IsExistByID(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static bool IsAppointmentScheduled(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID AND Status = 0";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static bool IsAppointmentAttended(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID AND Status = 1";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static bool IsAppointmentCancelled(int? id)
        {
            if (id == int.MinValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Appointments]
                WHERE AppointmentID = @AppointmentID AND Status = 3";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static int? Create(
            int? doctorID,
            int patientID,
            DateTime startAt,
            DateTime endAt,
            byte status,
            string? notes,
            int createdBy
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

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

                    object result = command.ExecuteScalar();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static bool Update(
            int? id,
            int? doctorID,
            int patientID,
            DateTime startAt,
            DateTime endAt,
            byte status,
            string? notes
        )
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

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

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        public static bool Delete(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"DELETE FROM [dbo].[Appointments] WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        public static bool HasDoctorExistingAppointment(int? doctorId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
        {
            if (!doctorId.HasValue)
                return false;

            bool hasOverlap = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

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

                    object result = command.ExecuteScalar();

                    hasOverlap = result != null;
                }
            }

            return hasOverlap;
        }

        public static bool HasPatientExistingAppointment(int patientId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
        {
            bool hasOverlap = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

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

                    object result = command.ExecuteScalar();

                    hasOverlap = result != null;
                }
            }

            return hasOverlap;
        }

        public static bool AttendAppointment(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Appointments]
                SET [Status] = 1
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        public static bool UnattendAppointment(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Appointments]
                SET [Status] = 2
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        public static bool CancelAppointment(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Appointments]
                SET [Status] = 3
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }
    }
}
