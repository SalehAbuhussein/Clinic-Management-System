using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsUserDA
    {
        public static DataTable FindUsers(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT
	                U.UserID,
	                Fullname = CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname),
	                U.Email,
	                U.Role,
	                U.Active
                FROM Users U
                INNER JOIN People P ON U.PersonID = P.PersonID
                ORDER BY UserID
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
            ref string passwordHash,
            ref string role,
            ref string email,
            ref bool active,
            ref int personID,
            ref string? refreshTokenHash,
            ref DateTime? refreshTokenExpiresAt,
            ref DateTime? refreshTokenRevokedAt
        )
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT 
                    [UserID]
                    ,[PasswordHash]
                    ,[Role]
                    ,[Email]
                    ,[Active]
                    ,[PersonID]
                    ,[RefreshTokenHash]
                    ,[RefreshTokenExpiresAt]
                    ,[RefreshTokenRevokedAt]
                FROM [dbo].[Users]
                WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            passwordHash = reader["PasswordHash"].ToString() ?? "";
                            role = reader["Role"].ToString() ?? "";
                            email = reader["Email"].ToString() ?? "";
                            active = (bool)reader["Active"];
                            personID = (int)reader["PersonID"];
                            refreshTokenHash = reader["RefreshTokenHash"] == DBNull.Value ? null : reader["RefreshTokenHash"].ToString();
                            if (reader["RefreshTokenExpiresAt"] != DBNull.Value)
                                refreshTokenExpiresAt = (DateTime)reader["RefreshTokenExpiresAt"];
                            else
                                refreshTokenExpiresAt = null;

                            if (reader["RefreshTokenRevokedAt"] != DBNull.Value)
                                refreshTokenRevokedAt = (DateTime)reader["RefreshTokenRevokedAt"];
                            else
                                refreshTokenRevokedAt = null;
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool FindByEmail(
            string email,
            ref string passwordHash,
            ref string role,
            ref int id,
            ref bool active,
            ref int personID,
            ref string? refreshTokenHash,
            ref DateTime? refreshTokenExpiresAt,
            ref DateTime? refreshTokenRevokedAt
        )
        {
            if (string.IsNullOrEmpty(email))
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT 
                    [UserID]
                    ,[PasswordHash]
                    ,[Role]
                    ,[Email]
                    ,[Active]
                    ,[PersonID]
                    ,[RefreshTokenHash]
                    ,[RefreshTokenExpiresAt]
                    ,[RefreshTokenRevokedAt]
                FROM [dbo].[Users]
                WHERE Email = @Email";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            id = (int)reader["UserID"];
                            passwordHash = reader["PasswordHash"].ToString() ?? "";
                            role = reader["Role"].ToString() ?? "";
                            email = reader["Email"].ToString() ?? "";
                            active = (bool)reader["Active"];
                            personID = (int)reader["PersonID"];
                            refreshTokenHash = reader["RefreshTokenHash"] == DBNull.Value ? null : reader["RefreshTokenHash"].ToString();
                            if (reader["RefreshTokenExpiresAt"] != DBNull.Value)
                                refreshTokenExpiresAt = (DateTime)reader["RefreshTokenExpiresAt"];
                            else
                                refreshTokenExpiresAt = null;

                            if (reader["RefreshTokenRevokedAt"] != DBNull.Value)
                                refreshTokenRevokedAt = (DateTime)reader["RefreshTokenRevokedAt"];
                            else
                                refreshTokenRevokedAt = null;
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool FindByPersonID(
            int? personID,
            ref string passwordHash,
            ref string role,
            ref int id,
            ref bool active,
            ref string email,
            ref string? refreshTokenHash,
            ref DateTime? refreshTokenExpiresAt,
            ref DateTime? refreshTokenRevokedAt
        )
        {
            if (!personID.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT 
                    [UserID]
                    ,[PasswordHash]
                    ,[Role]
                    ,[Email]
                    ,[Active]
                    ,[PersonID]
                    ,[RefreshTokenHash]
                    ,[RefreshTokenExpiresAt]
                    ,[RefreshTokenRevokedAt]
                FROM [dbo].[Users]
                WHERE PersonID = @PersonID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PersonID", personID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            id = (int)reader["UserID"];
                            passwordHash = reader["PasswordHash"].ToString() ?? "";
                            role = reader["Role"].ToString() ?? "";
                            email = reader["Email"].ToString() ?? "";
                            active = (bool)reader["Active"];
                            refreshTokenHash = reader["RefreshTokenHash"] == DBNull.Value ? null : reader["RefreshTokenHash"].ToString();
                            if (reader["RefreshTokenExpiresAt"] != DBNull.Value)
                                refreshTokenExpiresAt = (DateTime)reader["RefreshTokenExpiresAt"];
                            else
                                refreshTokenExpiresAt = null;

                            if (reader["RefreshTokenRevokedAt"] != DBNull.Value)
                                refreshTokenRevokedAt = (DateTime)reader["RefreshTokenRevokedAt"];
                            else
                                refreshTokenRevokedAt = null;
                        }
                    }
                }
            }

            return isFound;
        }

        public static int? Create(
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
            string email,
            string? refreshTokenHash,
            DateTime? refreshTokenExpiresAt,
            DateTime? refreshTokenRevokedAt
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("CreateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // User Info
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    //command.Parameters.AddWithValue("@RefreshTokenHash", string.IsNullOrEmpty(refreshTokenHash) ? DBNull.Value : refreshTokenHash);
                    //command.Parameters.AddWithValue("@RefreshTokenExpiresAt", refreshTokenExpiresAt == null ? DBNull.Value : refreshTokenExpiresAt);
                    //command.Parameters.AddWithValue("@RefreshTokenRevokedAt", refreshTokenRevokedAt == null ? DBNull.Value : refreshTokenRevokedAt);

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

                    SqlParameter userId = new SqlParameter("@UserID", SqlDbType.Int);
                    userId.Direction = ParameterDirection.Output;
                    command.Parameters.Add(userId);

                    command.ExecuteNonQuery();

                    id = (int)userId.Value;
                }
            }

            return id;
        }

        public static bool Update(
            int? id,
            string passwordHash,
            string role,
            string email,
            bool active,
            int? personID,
            string? refreshTokenHash,
            DateTime? refreshTokenExpiresAt,
            DateTime? refreshTokenRevokedAt
        )
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Users]
                    SET [PasswordHash] = @PasswordHash
                        ,[Role] = @Role
                        ,[Email] = @Email
                        ,[Active] = @Active
                        ,[PersonID] = @PersonID
                        ,[RefreshTokenHash] = @RefreshTokenHash
                        ,[RefreshTokenExpiresAt] = @RefreshTokenExpiresAt
                        ,[RefreshTokenRevokedAt] = @RefreshTokenRevokedAt
                    WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Role", role);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Active", active);
                    command.Parameters.AddWithValue("@PersonID", personID);
                    command.Parameters.AddWithValue("@RefreshTokenHash", string.IsNullOrEmpty(refreshTokenHash) ? DBNull.Value : refreshTokenHash);
                    command.Parameters.AddWithValue("@RefreshTokenExpiresAt", refreshTokenExpiresAt == null ? DBNull.Value : refreshTokenExpiresAt);
                    command.Parameters.AddWithValue("@RefreshTokenRevokedAt", refreshTokenRevokedAt == null ? DBNull.Value : refreshTokenRevokedAt);

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

                string query = @"DELETE FROM [dbo].[Users] WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        public static bool Deactivate(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Users]
                    SET
                        [Active] = 0
                        ,[RefreshTokenHash] = NULL
                        ,[RefreshTokenExpiresAt] = NULL
                        ,[RefreshTokenRevokedAt] = SYSUTCDATETIME()
                    WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        public static bool Activate(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Users]
                    SET [Active] = 1
                    WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }
    }
}
