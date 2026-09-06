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
        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT *
                FROM Patients
                ORDER BY PatientID
                OFFSET (@PageNumber - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

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
            int? id,
            ref string emergencyContactNumber,
            ref string emergencyContactName,
            ref DateTime createdAt,
            ref DateTime updatedAt,
            ref int createdBy,
            ref int personID,
            ref bool active
        )
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT *
                FROM [dbo].[Patients]
                WHERE PatientID = @PatientID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PatientID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            emergencyContactName = reader["EmergencyContactName"].ToString() ?? "";
                            emergencyContactNumber = reader["EmergencyContactNumber"].ToString() ?? "";
                            createdAt = (DateTime)reader["CreatedAt"];
                            updatedAt = (DateTime)reader["UpdatedAt"];
                            createdBy = (int)reader["CreatedBy"];
                            personID = (int)reader["PersonID"];
                            active = (bool)reader["Active"];
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool FindByNationalNo(
            string nationalNo,
            ref int? id,
            ref string emergencyContactNumber,
            ref string emergencyContactName,
            ref DateTime createdAt,
            ref DateTime updatedAt,
            ref int createdBy,
            ref int personID,
            ref bool active
        )
        {
            if (string.IsNullOrEmpty(nationalNo))
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT Patients.* FROM Patients
                INNER JOIN People ON Patients.PersonID = People.PersonID
                WHERE People.NationalNo = @NationalNo";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NationalNo", nationalNo);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            id = (int)reader["PatientID"];
                            emergencyContactName = reader["EmergencyContactName"].ToString() ?? "";
                            emergencyContactNumber = reader["EmergencyContactNumber"].ToString() ?? "";
                            createdAt = (DateTime)reader["CreatedAt"];
                            updatedAt = (DateTime)reader["UpdatedAt"];
                            createdBy = (int)reader["CreatedBy"];
                            personID = (int)reader["PersonID"];
                            active = (bool)reader["Active"];
                        }
                    }
                }
            }

            return isFound;
        }

        public static int? Create(
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
                connection.Open();

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

                    command.ExecuteNonQuery();

                    id = (int)patientID.Value;
                }
            }

            return id;
        }

        public static bool Update(
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
                connection.Open();

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

                    command.ExecuteNonQuery();

                    int result = (int)returnValue.Value;

                    hasUpdated = result == 1;
                }
            }

            return hasUpdated;
        }

        public static bool Delete(int? id)
        {
            if (!id.HasValue)
                return false;

            int result = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("DeletePatient", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@PatientID", id);

                    SqlParameter returnValue = command.Parameters.Add(
                        "@ReturnValue",
                        SqlDbType.Int
                    );
                    returnValue.Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    result = (int)returnValue.Value;
                }
            }

            return result == 1;
        }
    }
}
