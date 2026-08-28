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
        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT *
                FROM People
                ORDER BY PersonID
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
            ref string firstname,
            ref string midname,
            ref string? thirdname,
            ref string lastname,
            ref string nationalNo,
            ref string phone,
            ref bool gender,
            ref DateTime dateOfBirth,
            ref string? profileImage
        )
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT * FROM People WHERE PersonID = @PersonID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PersonID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            firstname = reader["Firstname"].ToString() ?? "";
                            midname = reader["Midname"].ToString() ?? "";
                            thirdname = reader["Thirdname"] != DBNull.Value ? reader["Thirdname"].ToString() : null;
                            lastname = reader["Lastname"].ToString() ?? string.Empty;
                            nationalNo = reader["NationalNo"].ToString() ?? string.Empty;
                            phone = reader["Phone"].ToString() ?? "";
                            gender = Convert.ToBoolean(reader["Gender"]);
                            dateOfBirth = Convert.ToDateTime(reader["DateOfBirth"]);
                            profileImage = reader["ProfileImage"]?.ToString();
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool Delete(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"DELETE FROM [dbo].[People] WHERE PersonID = @PersonID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PersonID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }
    }
}
