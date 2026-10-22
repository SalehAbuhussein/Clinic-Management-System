using Clinic_DataAccess.Common.Helpers;
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
        public static async Task<(List<MedicineRow> Rows, int Total)> FindAllAsync(string? name = null, int pageNumber = 1, int pageSize = 10)
        {
            List<MedicineRow> rows = new List<MedicineRow>();
            int total = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT COUNT(*) OVER() AS TotalCount,
                    [MedicineID], [RxCUI], [Genericname], [Brandname], [Form], [Strength]
                FROM Medicines";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        query += "\nWHERE Genericname LIKE @Name OR Brandname LIKE @Name";
                        command.Parameters.AddWithValue("@Name", clsSpecialCharacterEscaper.Contains(name));
                    }

                    query += @"
                        ORDER BY Genericname, MedicineID
                        OFFSET (@PageNumber - 1) * @PageSize ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";

                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    command.CommandText = query;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            total = (int)reader["TotalCount"];

                            rows.Add(new MedicineRow(
                                (int)reader["MedicineID"],
                                (int)reader["RxCUI"],
                                reader["Genericname"].ToString() ?? string.Empty,
                                reader["Brandname"] as string,
                                reader["Form"].ToString() ?? string.Empty,
                                reader["Strength"].ToString() ?? string.Empty
                            ));
                        }
                    }
                }
            }

            return (rows, total);
        }

        /// <summary>
        /// One medicine as the database has it. A record rather than a DataRow so callers get
        /// compile-time field names instead of reader["Genericname"], and rather than ref
        /// parameters because ref cannot cross an await (CS1988).
        /// </summary>
        public record MedicineRow(
            int MedicineID,
            int RxCUI,
            string Genericname,
            string? Brandname,
            string Form,
            string Strength
        );

        public static async Task<MedicineRow?> FindAsync(int? id)
        {
            if (!id.HasValue)
                return null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT [MedicineID], [RxCUI], [Genericname], [Brandname], [Form], [Strength]
                FROM [ClinicManagementSystem].[dbo].[Medicines]
                WHERE MedicineID = @MedicineID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MedicineID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            return null;

                        return new MedicineRow(
                            (int)reader["MedicineID"],
                            (int)reader["RxCUI"],
                            reader["Genericname"].ToString() ?? string.Empty,
                            reader["Brandname"] as string,
                            reader["Form"].ToString() ?? string.Empty,
                            reader["Strength"].ToString() ?? string.Empty
                        );
                    }
                }
            }
        }

        public static async Task<bool> AreValidMedicinesAsync(List<string> medicineIds)
        {
            bool isValid = false;

            medicineIds = medicineIds.Distinct().ToList();

            if (medicineIds.Count == 0)
                return false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT MedicineCount = COUNT(DISTINCT MedicineID)
                FROM Medicines
                WHERE MedicineID IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@MedicineIds, ','))
                HAVING COUNT(DISTINCT MedicineID) = @MedicineIdsCount";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    string[] medicineIdsArr = medicineIds.ToArray();

                    command.Parameters.AddWithValue("@MedicineIds", string.Join(",", medicineIdsArr));
                    command.Parameters.AddWithValue("@MedicineIdsCount", medicineIdsArr.Length);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        isValid = reader.HasRows;
                    }
                }
            }

            return isValid;
        }

        public static async Task<int?> CreateAsync(
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
                await connection.OpenAsync();

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

                    object? result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(
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
                await connection.OpenAsync();

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

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> DeleteAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"DELETE FROM [dbo].[Medicines] WHERE MedicineID = @MedicineID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MedicineID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }
    }
}
