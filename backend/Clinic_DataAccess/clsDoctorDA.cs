using Clinic_DataAccess.Common;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsDoctorDA
    {

        /// <summary>
        /// The WHERE clause the list filters by, with its parameters added to
        /// the command. The list and GetTotalCount share it, so a filter added
        /// to one applies to the other and the two cannot disagree.
        /// </summary>
        /// <param name="command">The command the parameters go on.</param>
        /// <returns>The clause to append, or an empty string when nothing is filtered.</returns>
        private static string _DoctorFilters(SqlCommand command, string? fullname, string? specialization, bool? active)
        {
            List<string> conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(fullname))
            {
                conditions.Add("CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname) LIKE @Fullname");
                command.Parameters.AddWithValue("@Fullname", clsLikePattern.Contains(fullname));
            }

            if (!string.IsNullOrWhiteSpace(specialization))
            {
                conditions.Add("D.Specialization LIKE @Specialization");
                command.Parameters.AddWithValue("@Specialization", clsLikePattern.Contains(specialization));
            }

            if (active.HasValue)
            {
                conditions.Add("U.Active = @Active");
                command.Parameters.AddWithValue("@Active", active.Value);
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
        public static async Task<int> GetTotalCount(string? fullname = null, string? specialization = null, bool? active = null)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    string query = @"SELECT COUNT(*)
                    FROM Doctors D
                    INNER JOIN Users U ON D.UserID = U.UserID
                    INNER JOIN People P ON P.PersonID = U.PersonID";

                    command.CommandText = query + _DoctorFilters(command, fullname, specialization, active);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public static async Task<DataTable> FindAllAsync(string? fullname = null, string? specialization = null, bool? active = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,

                    D.DoctorID, D.Specialization, D.MedicalSchool, D.GraduationYear,
                    U.PersonID, U.Active,

                    CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname) AS Fullname,
                    P.Firstname, P.Midname, P.Thirdname, P.Lastname,
                    P.NationalNo, P.Phone, P.Gender, P.DateOfBirth

                FROM Doctors D
                INNER JOIN Users U ON D.UserID = U.UserID
                INNER JOIN People P ON P.PersonID = U.PersonID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    query += _DoctorFilters(command, fullname, specialization, active);

                    query += @"
                        ORDER BY D.DoctorID
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
        /// A doctor with its user and person, in one round trip. Columns are named rather
        /// than SELECT *: Doctors and Users both have UserID, Users and People both have
        /// PersonID, and ADO.NET would silently rename the duplicates to UserID1/PersonID1.
        /// </summary>
        /// <summary>
        /// A doctor with its user and person, in one round trip. Find and FindByUserID
        /// differ only in their WHERE, so they share this query. The column name is a
        /// compile-time literal from the two callers below, never from a request.
        /// </summary>
        private static async Task<DataRow?> _FindByAsync(string whereColumn, object value)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = $@"SELECT
                    D.DoctorID, D.UserID, D.Specialization, D.MedicalSchool,
                    D.GraduationYear, D.CreatedAt, D.UpdatedAt, D.CreatedBy,
                    U.PersonID, U.Email, U.PasswordHash, U.Role, U.Active,
                    P.Firstname, P.Midname, P.Thirdname, P.Lastname,
                    P.NationalNo, P.Phone, P.Gender, P.DateOfBirth, P.ProfileImage
                FROM Doctors D
                INNER JOIN Users U ON U.UserID = D.UserID
                INNER JOIN People P ON P.PersonID = U.PersonID
                WHERE {whereColumn} = @Value";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Value", value);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static Task<DataRow?> FindAsync(int? id)
        {
            return id.HasValue ? _FindByAsync("D.DoctorID", id.Value) : Task.FromResult<DataRow?>(null);
        }

        public static Task<DataRow?> FindByUserIDAsync(int? userID)
        {
            return userID.HasValue ? _FindByAsync("D.UserID", userID.Value) : Task.FromResult<DataRow?>(null);
        }


        public static async Task<(int? doctorID, int? personID, int? userID)> CreateAsync(
            string specialization,
            string medicalSchool,
            ushort graduationYear,
            int createdBy,

            string firstname,
            string midname,
            string? thirdname,
            string lastname,
            string nationalNo,
            string phone,
            bool? gender,
            DateOnly dateOfBirth,
            string? profileImage,

            string passwordHash,
            string email
        )
        {
            (int? doctorID, int? personID, int? userID) = (null, null, null);

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("CreateDoctor", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // User Info
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    // Person Info
                    command.Parameters.AddWithValue("@Firstname", firstname);
                    command.Parameters.AddWithValue("@Midname", midname);
                    command.Parameters.AddWithValue("@Thirdname", thirdname);
                    command.Parameters.AddWithValue("@Lastname", lastname);
                    command.Parameters.AddWithValue("@NationalNo", nationalNo);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Gender", gender);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    command.Parameters.AddWithValue("@ProfileImage", profileImage);


                    // Doctor Info
                    command.Parameters.AddWithValue("@Specialization", specialization);
                    command.Parameters.AddWithValue("@MedicalSchool", medicalSchool);
                    command.Parameters.AddWithValue("@GraduationYear", Convert.ToInt32(graduationYear));
                    command.Parameters.AddWithValue("@CreatedBy", createdBy);

                    SqlParameter doctorIDOutput = new SqlParameter("@DoctorID", SqlDbType.Int);
                    doctorIDOutput.Direction = ParameterDirection.Output;
                    command.Parameters.Add(doctorIDOutput);

                    SqlParameter personIDOutput = new SqlParameter("@PersonID", SqlDbType.Int);
                    personIDOutput.Direction = ParameterDirection.Output;
                    command.Parameters.Add(personIDOutput);

                    SqlParameter userIDOutput = new SqlParameter("@UserID", SqlDbType.Int);
                    userIDOutput.Direction = ParameterDirection.Output;
                    command.Parameters.Add(userIDOutput);

                    await command.ExecuteNonQueryAsync();

                    doctorID = (int)doctorIDOutput.Value;
                    personID = (int)personIDOutput.Value;
                    userID = (int)userIDOutput.Value;
                }
            }

            return (doctorID, personID, userID);
        }

        public static async Task<bool> UpdateAsync(
            int? id,
            int? personId,
            string specialization,
            string medicalSchool,
            ushort graduationYear,
            string firstname,
            string midname,
            string? thirdname,
            string lastname,
            string phone,
            bool? gender,
            DateOnly dateOfBirth,
            string? profileImage
        )
        {
            if (!id.HasValue)
                return false;

            bool hasUpdated = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("UpdateDoctor", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Person Info
                    command.Parameters.AddWithValue("@PersonID", personId);
                    command.Parameters.AddWithValue("@Firstname", firstname);
                    command.Parameters.AddWithValue("@Midname", midname);
                    command.Parameters.AddWithValue("@Thirdname", thirdname);
                    command.Parameters.AddWithValue("@Lastname", lastname);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Gender", gender);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    command.Parameters.AddWithValue("@ProfileImage", string.IsNullOrEmpty(profileImage) ? DBNull.Value : profileImage);

                    // Doctor Info
                    command.Parameters.AddWithValue("@DoctorID", id);
                    command.Parameters.AddWithValue("@Specialization", specialization);
                    command.Parameters.AddWithValue("@MedicalSchool", medicalSchool);
                    command.Parameters.AddWithValue("@GraduationYear", Convert.ToInt32(graduationYear));

                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    command.Parameters.Add(returnValue);

                    await command.ExecuteNonQueryAsync();

                    int result = (int)returnValue.Value;

                    hasUpdated = result == 1;
                }
            }

            return hasUpdated;
        }

        public static async Task<bool> DeleteAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            using (SqlConnection connection = new SqlConnection( clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("DeleteDoctor", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@DoctorID", id);

                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    command.Parameters.Add(returnValue);

                    await command.ExecuteNonQueryAsync();

                    int result = (int)returnValue.Value;

                    return result == 1;
                }
            }
        }

        public static async Task<bool> IsDoctorOfPatientAsync(int? patientId, int? userId)
        {
            if (!patientId.HasValue || !userId.HasValue)
                return false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT 1
                FROM Appointments A
                INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                WHERE A.PatientID = @PatientID AND D.UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PatientID", patientId);
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        return reader.HasRows;
                }
            }
        }

        public static async Task<bool> IsDoctorOfVisitAsync(int? visitId, int? userId)
        {
            if (!visitId.HasValue || !userId.HasValue)
                return false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT 1
                FROM Visits V
                INNER JOIN Appointments A ON A.AppointmentID = V.AppointmentID
                INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                WHERE V.VisitID = @VisitID AND D.UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitId);
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        return reader.HasRows;
                }
            }
        }
    }
}
