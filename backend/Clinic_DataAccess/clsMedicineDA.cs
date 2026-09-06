using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsMedicineDA
    {
        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT *
                FROM Medicines
                ORDER BY MedicineID
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
            ref int rxCUI,
            ref string genericName,
            ref string brandName,
            ref string form,
            ref string strength
        )
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT [MedicineID]
                    ,[RxCUI]
                    ,[Genericname]
	                ,[Brandname]
	                ,[Form]
	                ,[Strength]
                FROM [ClinicManagementSystem].[dbo].[Medicines]
                WHERE MedicineID = @MedicineID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MedicineID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            rxCUI = (int)reader["RxCUI"];
                            genericName = reader["Genericname"].ToString() ?? "";
                            brandName = reader["Brandname"].ToString() ?? "";
                            form = reader["Form"].ToString() ?? "";
                            strength = reader["Strength"].ToString() ?? "";
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool IsValidMedicines(List<string> medicineIds)
        {
            bool isValid = false;

            medicineIds = medicineIds.Distinct().ToList();

            if (medicineIds.Count == 0)
                return false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT MedicineCount = COUNT(DISTINCT MedicineID)
                FROM Medicines
                WHERE MedicineID IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@MedicineIds, ','))
                HAVING COUNT(DISTINCT MedicineID) = @MedicineIdsCount";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    string[] medicineIdsArr = medicineIds.ToArray();

                    command.Parameters.AddWithValue("@MedicineIds", string.Join(",", medicineIdsArr));
                    command.Parameters.AddWithValue("@MedicineIdsCount", medicineIdsArr.Length);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        isValid = reader.HasRows;
                    }
                }
            }

            return isValid;
        }

        public static int? Create(
            int rxCUI,
            string genericName,
            string? brandName,
            string form,
            string strength
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"INSERT INTO [dbo].[Medicines]
                        ([RxCUI]
                        ,[Genericname]
                        ,[Brandname]
                        ,[Form]
                        ,[Strength])
                VALUES
                        (@RxCUI
                        ,@Genericname
                        ,@Brandname
                        ,@Form
                        ,@Strength); SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Genericname", genericName);
                    command.Parameters.AddWithValue("@Brandname", string.IsNullOrEmpty(brandName) ? DBNull.Value : brandName);
                    command.Parameters.AddWithValue("@Form", form);
                    command.Parameters.AddWithValue("@Strength", strength);
                    command.Parameters.AddWithValue("@RxCUI", rxCUI);

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
            int rxCUI,
            string genericName,
            string? brandName,
            string form,
            string strength
        )
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Medicines]
                SET [RxCUI] = @RxCUI
                    ,[Genericname] = @Genericname
                    ,[Brandname] = @Brandname
                    ,[Form] = @Form
                    ,[Strength] = @Strength
                WHERE MedicineID = @MedicineID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MedicineID", id);
                    command.Parameters.AddWithValue("@RxCUI", rxCUI);
                    command.Parameters.AddWithValue("@Genericname", genericName);
                    command.Parameters.AddWithValue("@Brandname", string.IsNullOrEmpty(brandName) ? DBNull.Value : brandName);
                    command.Parameters.AddWithValue("@Form", form);
                    command.Parameters.AddWithValue("@Strength", strength);

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

                string query = @"DELETE FROM [dbo].[Medicines] WHERE MedicineID = @MedicineID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MedicineID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }
    }
}
