using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsPersonDA
    {
        public static async Task<DataTable> FindAllAsync(string? fullname = null, string? nationalNo = null, string? phone = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT COUNT(*) OVER() AS TotalCount
                    ,[PersonID]
                    ,[Firstname]
                    ,[Midname]
                    ,[Thirdname]
                    ,[Lastname]
                    ,[NationalNo]
                    ,[Phone]
                    ,[Gender]
                    ,[DateOfBirth]
                    ,[ProfileImage]
                FROM [dbo].[People]";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    List<string> conditions = new List<string>();

                    if (!string.IsNullOrWhiteSpace(fullname))
                    {
                        conditions.Add("CONCAT_WS(' ', Firstname, Midname, Thirdname, Lastname) LIKE @Fullname");
                        command.Parameters.AddWithValue("@Fullname", $"%{fullname.Trim()}%");
                    }

                    if (!string.IsNullOrWhiteSpace(nationalNo))
                    {
                        conditions.Add("NationalNo LIKE @NationalNo");
                        command.Parameters.AddWithValue("@NationalNo", $"{nationalNo.Trim()}%");
                    }

                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        conditions.Add("Phone LIKE @Phone");
                        command.Parameters.AddWithValue("@Phone", $"{phone.Trim()}%");
                    }

                    if (conditions.Count > 0)
                        query += $"\nWHERE {string.Join(" AND ", conditions)}";

                    query += @"
                        ORDER BY PersonID
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

        /// <summary>Returns the row; ten ref parameters cannot cross an await (CS1988).</summary>
        public static async Task<DataRow?> FindAsync(int? id)
        {
            if (!id.HasValue)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT [PersonID]
                    ,[Firstname]
                    ,[Midname]
                    ,[Thirdname]
                    ,[Lastname]
                    ,[NationalNo]
                    ,[Phone]
                    ,[Gender]
                    ,[DateOfBirth]
                    ,[ProfileImage]
                FROM [dbo].[People]
                WHERE PersonID = @PersonID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PersonID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static async Task<bool> UpdateAsync(
            int? id,
            string firstname,
            string midname,
            string? thirdname,
            string lastname,
            string nationalNo,
            string phone,
            bool gender,
            DateOnly dateOfBirth,
            string? profileImage
        )
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[People]
                    SET [Firstname] = @Firstname
                       ,[Midname] = @Midname
                       ,[Thirdname] = @Thirdname
                       ,[Lastname] = @Lastname
                       ,[NationalNo] = @NationalNo
                       ,[Phone] = @Phone
                       ,[Gender] = @Gender
                       ,[DateOfBirth] = @DateOfBirth
                       ,[ProfileImage] = @ProfileImage
                WHERE PersonID = @PersonID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PersonID", id);
                    command.Parameters.AddWithValue("@Firstname", firstname);
                    command.Parameters.AddWithValue("@Midname", midname);
                    command.Parameters.AddWithValue("@Thirdname", (object?)thirdname ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Lastname", lastname);
                    command.Parameters.AddWithValue("@NationalNo", nationalNo);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Gender", gender);
                    command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.ToDateTime(TimeOnly.MinValue));
                    command.Parameters.AddWithValue("@ProfileImage", (object?)profileImage ?? DBNull.Value);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> IsUsedAsync(int personId)
        {
            bool isUsed = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT TOP 1 1
                    WHERE EXISTS (SELECT 1 FROM Users    WHERE PersonID = @PersonID)
                       OR EXISTS (SELECT 1 FROM Patients WHERE PersonID = @PersonID);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PersonID", personId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        isUsed = reader.HasRows;
                    }
                }
            }

            return isUsed;
        }

        public static async Task<bool> DeleteAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"DELETE FROM [dbo].[People] WHERE PersonID = @PersonID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PersonID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }
    }
}
