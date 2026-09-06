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
        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT *
                FROM Doctors D
                INNER JOIN Users U ON D.UserID = U.UserID
                INNER JOIN People P ON P.PersonID = U.PersonID
                ORDER BY D.DoctorID
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

        /// <summary>
        /// A doctor with its user and person, in one round trip. Columns are named rather
        /// than SELECT *: Doctors and Users both have UserID, Users and People both have
        /// PersonID, and ADO.NET would silently rename the duplicates to UserID1/PersonID1.
        /// </summary>
        public static DataRow? Find(int? id)
        {
            if (!id.HasValue)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT
                    D.DoctorID, D.UserID, D.Specialization, D.MedicalSchool,
                    D.GraduationYear, D.CreatedAt, D.UpdatedAt, D.CreatedBy,
                    U.PersonID, U.Email, U.PasswordHash, U.Role, U.Active,
                    P.Firstname, P.Midname, P.Thirdname, P.Lastname,
                    P.NationalNo, P.Phone, P.Gender, P.DateOfBirth, P.ProfileImage
                FROM Doctors D
                INNER JOIN Users U ON U.UserID = D.UserID
                INNER JOIN People P ON P.PersonID = U.PersonID
                WHERE D.DoctorID = @DoctorID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DoctorID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static bool FindByUserID(
            int? id,
            ref int doctorID,
            ref string specialization,
            ref string medicalSchool,
            ref ushort graduationYear,
            ref DateTime createdAt,
            ref DateTime updatedAt,
            ref int createdBy
        )
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT [DoctorID]
                    ,[Specialization]
                    ,[MedicalSchool]
                    ,[GraduationYear]
                    ,[CreatedAt]
                    ,[UpdatedAt]
                    ,[CreatedBy]
                    ,[UserID]
                FROM [dbo].[Doctors]
                WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;
                            doctorID = (int)reader["DoctorID"];
                            specialization = reader["Specialization"].ToString() ?? "";
                            medicalSchool = reader["MedicalSchool"].ToString() ?? "";
                            graduationYear = Convert.ToUInt16(reader["GraduationYear"]);
                            createdAt = (DateTime)reader["CreatedAt"];
                            updatedAt = (DateTime)reader["UpdatedAt"];
                            createdBy = (int)reader["CreatedBy"];
                        }
                    }
                }
            }

            return isFound;
        }

        public static (int? doctorID, int? personID, int? userID) Create(
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
            DateTime dateOfBirth,
            string? profileImage,

            string passwordHash,
            string email
        )
        {
            (int? doctorID, int? personID, int? userID) = (null, null, null);

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

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

                    command.ExecuteNonQuery();
                        
                    doctorID = (int)doctorIDOutput.Value;
                    personID = (int)personIDOutput.Value;
                    userID = (int)userIDOutput.Value;
                }
            }

            return (doctorID, personID, userID);
        }

        public static bool Update(
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

            using (SqlConnection connection = new SqlConnection( clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("DeleteDoctor", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@DoctorID", id);

                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    command.Parameters.Add(returnValue);

                    command.ExecuteNonQuery();

                    int result = (int)returnValue.Value;

                    return result == 1;
                }
            }
        }

        public static bool IsDoctorOfVisit(int? visitId, int? userId)
        {
            if (!visitId.HasValue || !userId.HasValue)
                return false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT 1
                FROM Visits V
                INNER JOIN Appointments A ON A.AppointmentID = V.AppointmentID
                INNER JOIN Doctors D ON D.DoctorID = A.DoctorID
                WHERE V.VisitID = @VisitID AND D.UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitId);
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (SqlDataReader reader = command.ExecuteReader())
                        return reader.HasRows;
                }
            }
        }
    }
}
