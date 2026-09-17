using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsPatientDA
    {
        public static async Task<DataTable> FindAllAsync(string? fullname = null, string? phone = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,

                    PT.PatientID, PT.EmergencyContactNumber, PT.EmergencyContactName,
                    PT.CreatedAt, PT.UpdatedAt, PT.CreatedBy, PT.Active, PT.PersonID,

                    CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname) AS Fullname,
                    P.Firstname, P.Midname, P.Thirdname, P.Lastname,
                    P.NationalNo, P.Phone, P.Gender, P.DateOfBirth, P.ProfileImage

                FROM Patients PT
                INNER JOIN People P ON P.PersonID = PT.PersonID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    List<string> conditions = new List<string>();

                    if (!string.IsNullOrWhiteSpace(fullname))
                    {
                        conditions.Add("CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname) LIKE @Fullname");
                        command.Parameters.AddWithValue("@Fullname", $"%{fullname.Trim()}%");
                    }

                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        // Anchored at the start: nobody searches a phone number from the
                        // middle, and a leading wildcard would rule out an index seek.
                        conditions.Add("P.Phone LIKE @Phone");
                        command.Parameters.AddWithValue("@Phone", $"{phone.Trim()}%");
                    }

                    if (conditions.Count > 0)
                        query += $"\nWHERE {string.Join(" AND ", conditions)}";

                    query += @"
                        ORDER BY PT.PatientID
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
        /// A patient with its person, in one round trip. Find and FindByNationalNo differ
        /// only in their WHERE, so they share this query. The column name is a compile-time
        /// literal from the two callers below, never from a request.
        /// </summary>
        private static async Task<DataRow?> _FindByAsync(string whereColumn, object value)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = $@"SELECT
                    PT.PatientID, PT.EmergencyContactNumber, PT.EmergencyContactName,
                    PT.CreatedAt, PT.UpdatedAt, PT.CreatedBy, PT.Active, PT.PersonID,
                    P.Firstname, P.Midname, P.Thirdname, P.Lastname,
                    P.NationalNo, P.Phone, P.Gender, P.DateOfBirth, P.ProfileImage
                FROM Patients PT
                INNER JOIN People P ON P.PersonID = PT.PersonID
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
            return id.HasValue ? _FindByAsync("PT.PatientID", id.Value) : Task.FromResult<DataRow?>(null);
        }

        public static Task<DataRow?> FindByNationalNoAsync(string nationalNo)
        {
            return string.IsNullOrEmpty(nationalNo) ? Task.FromResult<DataRow?>(null) : _FindByAsync("P.NationalNo", nationalNo);
        }


        public static async Task<int?> CreateAsync(
            string? emergencyContactNumber,
            string? emergencyContactName,
            DateTime createdAt,
            DateTime updatedAt,
            int createdBy,
            string firstname,
            string midname,
            string? thirdname,
            string lastname,
            string nationalNo,
            string phone,
            bool? gender,
            DateTime dateOfBirth,
            string? profileImage
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("CreatePatient", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Person Info
                    command.Parameters.AddWithValue("@Firstname", firstname);
                    command.Parameters.AddWithValue("@Midname", midname);
                    command.Parameters.AddWithValue("@Thirdname", thirdname);
                    command.Parameters.AddWithValue("@Lastname", lastname);
                    command.Parameters.AddWithValue("@NationalNo", nationalNo);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Gender", gender);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    command.Parameters.AddWithValue("@ProfileImage", string.IsNullOrEmpty(profileImage) ? DBNull.Value : profileImage);

                    // Patient Info
                    command.Parameters.AddWithValue("@EmergencyContactNumber", string.IsNullOrEmpty(emergencyContactNumber) ? DBNull.Value : emergencyContactNumber);
                    command.Parameters.AddWithValue("@EmergencyContactName", string.IsNullOrEmpty(emergencyContactName) ? DBNull.Value : emergencyContactName);
                    command.Parameters.AddWithValue("@CreatedBy", createdBy);

                    SqlParameter patientID = new SqlParameter("@PatientID", SqlDbType.Int);
                    patientID.Direction = ParameterDirection.Output;
                    command.Parameters.Add(patientID);

                    await command.ExecuteNonQueryAsync();

                    id = (int)patientID.Value;
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(
            int? id,
            string? emergencyContactNumber,
            string? emergencyContactName,
            int? personID,
            string firstname,
            string midname,
            string? thirdname,
            string lastname,
            string phone,
            bool? gender,
            DateTime dateOfBirth,
            string? profileImage
        )
        {
            if (!id.HasValue)
                return false;

            bool hasUpdated = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("UpdatePatient", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Person Info
                    command.Parameters.AddWithValue("@Firstname", firstname);
                    command.Parameters.AddWithValue("@Midname", midname);
                    command.Parameters.AddWithValue("@Thirdname", thirdname);
                    command.Parameters.AddWithValue("@Lastname", lastname);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Gender", gender);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    command.Parameters.AddWithValue("@ProfileImage", string.IsNullOrEmpty(profileImage) ? DBNull.Value : profileImage);

                    // Doctor Info
                    command.Parameters.AddWithValue("@PatientID", id);
                    command.Parameters.AddWithValue("@PersonID", personID);
                    command.Parameters.AddWithValue("@EmergencyContactNumber", string.IsNullOrEmpty(emergencyContactNumber) ? DBNull.Value : emergencyContactNumber);
                    command.Parameters.AddWithValue("@EmergencyContactName", string.IsNullOrEmpty(emergencyContactName) ? DBNull.Value : emergencyContactName);

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

            int result = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("DeletePatient", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@PatientID", id);

                    SqlParameter returnValue = command.Parameters.Add(
                        "@ReturnValue",
                        SqlDbType.Int
                    );
                    returnValue.Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync();

                    result = (int)returnValue.Value;
                }
            }

            return result == 1;
        }
    }
}
