using Clinic_DataAccess.Common.Models;
using Clinic_DataAccess.Common.Helpers;
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
        private static string _UserFilters(SqlCommand command, string? fullname, string? email, bool? active)
        {
            List<string> conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(fullname))
            {
                conditions.Add("CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname) LIKE @Fullname");
                command.Parameters.AddWithValue("@Fullname", clsSpecialCharacterEscaper.Contains(fullname));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                conditions.Add("U.Email LIKE @Email");
                command.Parameters.AddWithValue("@Email", clsSpecialCharacterEscaper.StartsWith(email));
            }

            if (active.HasValue)
            {
                conditions.Add("U.Active = @Active");
                command.Parameters.AddWithValue("@Active", active.Value);
            }

            return conditions.Count == 0 ? string.Empty : $"\nWHERE {string.Join(" AND ", conditions)}";
        }

        public static async Task<int> GetTotalCountAsync(string? fullname = null, string? email = null, bool? active = null)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    string query = @"SELECT COUNT(*)
                    FROM Users U
                    INNER JOIN People P ON U.PersonID = P.PersonID";

                    command.CommandText = query + _UserFilters(command, fullname, email, active);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public static async Task<DataTable> FindAllAsync(string? fullname = null, string? email = null, bool? active = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,

	                U.UserID,
	                Fullname = CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname),
	                U.Email,
	                U.Role,
	                U.Active
                FROM Users U
                INNER JOIN People P ON U.PersonID = P.PersonID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    query += _UserFilters(command, fullname, email, active);

                    query += @"
                        ORDER BY UserID
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

        private static async Task<DataRow?> _FindByAsync(string whereColumn, object value)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = $@"SELECT
                    U.UserID, U.PasswordHash, U.Role, U.Email, U.Active, U.PersonID,
                    U.RefreshTokenHash, U.RefreshTokenExpiresAt, U.RefreshTokenRevokedAt,
                    P.Firstname, P.Midname, P.Thirdname, P.Lastname,
                    P.NationalNo, P.Phone, P.Gender, P.DateOfBirth, P.ProfileImage
                FROM Users U
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

        public static Task<DataRow?> FindAsync(int? id) => id.HasValue ? _FindByAsync("U.UserID", id.Value) : Task.FromResult<DataRow?>(null);

        public static Task<DataRow?> FindByEmailAsync(string email) => string.IsNullOrEmpty(email) ? Task.FromResult<DataRow?>(null) : _FindByAsync("U.Email", email);

        public static Task<DataRow?> FindByPersonIDAsync(int? personID) => personID.HasValue ? _FindByAsync("U.PersonID", personID.Value) : Task.FromResult<DataRow?>(null);

        public static async Task<int?> CreateAsync(
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
            string email,
            string? refreshTokenHash,
            DateTime? refreshTokenExpiresAt,
            DateTime? refreshTokenRevokedAt,
            int? actorUserId,
            string auditAction,
            string? auditDetail
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
                using (SqlCommand command = new SqlCommand("CreateUser", connection, transaction))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // User Info
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    // Person Info
                    command.Parameters.AddWithValue("@Firstname", firstname);
                    command.Parameters.AddWithValue("@Midname", midname);
                    command.Parameters.AddWithValue("@Thirdname", (object?)thirdname ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Lastname", lastname);
                    command.Parameters.AddWithValue("@NationalNo", nationalNo);
                    command.Parameters.AddWithValue("@Phone", phone);
                    if (gender.HasValue)
                        command.Parameters.AddWithValue("@Gender", gender.Value);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    command.Parameters.AddWithValue("@ProfileImage", (object?)profileImage ?? DBNull.Value);

                    SqlParameter userId = new SqlParameter("@UserID", SqlDbType.Int);
                    userId.Direction = ParameterDirection.Output;
                    command.Parameters.Add(userId);

                    await command.ExecuteNonQueryAsync();

                    id = (int)userId.Value;

                    await clsAuditEntryDA.CreateAsync(connection, transaction,
                        [new AuditRow(actorUserId, auditAction, "User", id, auditDetail)]);

                    await transaction.CommitAsync();
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(
            int? id,
            string passwordHash,
            string role,
            string email,
            bool active,
            int? personID,
            string? refreshTokenHash,
            DateTime? refreshTokenExpiresAt,
            DateTime? refreshTokenRevokedAt,
            IReadOnlyList<AuditRow> auditRows
        )
        {
            if (!id.HasValue || !personID.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
                {
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

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
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

                        rowsAffected = await command.ExecuteNonQueryAsync();
                    }

                    if (rowsAffected == 0)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    await clsAuditEntryDA.CreateAsync(connection, transaction, auditRows);

                    await transaction.CommitAsync();
                }
            }

            return true;
        }

        public static async Task<bool> DeleteAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"DELETE FROM [dbo].[Users] WHERE UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> DeactivateAsync(int? id, IReadOnlyList<AuditRow> auditRows)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
                {
                    string query = @"UPDATE [dbo].[Users]
                        SET
                            [Active] = 0
                            ,[RefreshTokenHash] = NULL
                            ,[RefreshTokenExpiresAt] = NULL
                            ,[RefreshTokenRevokedAt] = SYSUTCDATETIME()
                        WHERE UserID = @UserID";

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@UserID", id);

                        rowsAffected = await command.ExecuteNonQueryAsync();
                    }

                    if (rowsAffected == 0)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    await clsAuditEntryDA.CreateAsync(connection, transaction, auditRows);

                    await transaction.CommitAsync();
                }
            }

            return true;
        }

        public static async Task<bool> ActivateAsync(int? id, IReadOnlyList<AuditRow> auditRows)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
                {
                    string query = @"UPDATE [dbo].[Users]
                        SET [Active] = 1
                        WHERE UserID = @UserID";

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@UserID", id);

                        rowsAffected = await command.ExecuteNonQueryAsync();
                    }

                    if (rowsAffected == 0)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    await clsAuditEntryDA.CreateAsync(connection, transaction, auditRows);

                    await transaction.CommitAsync();
                }
            }

            return true;
        }
    }
}
