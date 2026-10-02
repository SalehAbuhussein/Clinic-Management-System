using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsPrescriptionItemDA
    {
        public static async Task<DataTable> FindAllByVisitIDAsync(int visitID, int pageNumber, int pageSize)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT P.[PrescriptionItemID]
                    ,P.[VisitID]
                    ,P.[MedicineID]
                    ,P.[Dosage]
                    ,P.[Frequency]
                    ,P.[Duration]

                    ,M.[RxCUI]
                    ,M.[Genericname]
                    ,M.[Brandname]
                    ,M.[Form]
                    ,M.[Strength]
                FROM PrescriptionItems P
                INNER JOIN Medicines M ON M.MedicineID = P.MedicineID
                WHERE P.VisitID = @VisitID
                ORDER BY P.PrescriptionItemID
                OFFSET (@PageNumber - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitID);
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

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

        public static async Task<bool> HasPrescriptionByVisitIDAsync(int? id)
        {
            if (!id.HasValue)
                return false;

            bool hasPrescriptions = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT TOP 1 true=1 FROM PrescriptionItems WHERE VisitID = @VisitID;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        hasPrescriptions = reader.HasRows;
                    }
                }
            }

            return hasPrescriptions;
        }

        /// <summary>Returns the row; ref parameters cannot cross an await (CS1988).</summary>
        public static async Task<DataRow?> FindAsync(int id)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT [PrescriptionItemID]
                    ,[VisitID]
                    ,[MedicineID]
                    ,[Dosage]
                    ,[Frequency]
                    ,[Duration]
                FROM [dbo].[PrescriptionItems]
                WHERE PrescriptionItemID = @PrescriptionItemID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PrescriptionItemID", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static async Task<int?> CreateAsync(int visitID, int medicineID, string dosage, string frequency, string duration)
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO [dbo].[PrescriptionItems]
                    ([VisitID]
                    ,[MedicineID]
                    ,[Dosage]
                    ,[Frequency]
                    ,[Duration])
                    VALUES
                    (@VisitID
                    ,@MedicineID
                    ,@Dosage
                    ,@Frequency
                    ,@Duration); SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitID);
                    command.Parameters.AddWithValue("@MedicineID", medicineID);
                    command.Parameters.AddWithValue("@Dosage", dosage);
                    command.Parameters.AddWithValue("@Frequency", frequency);
                    command.Parameters.AddWithValue("@Duration", duration);

                    object? result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(int? id, int medicineID, string dosage, string frequency, string duration)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[PrescriptionItems]
                SET [MedicineID] = @MedicineID
                    ,[Dosage] = @Dosage
                    ,[Frequency] = @Frequency
                    ,[Duration] = @Duration
                WHERE PrescriptionItemID = @PrescriptionItemID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PrescriptionItemID", id);
                    command.Parameters.AddWithValue("@MedicineID", medicineID);
                    command.Parameters.AddWithValue("@Dosage", dosage);
                    command.Parameters.AddWithValue("@Frequency", frequency);
                    command.Parameters.AddWithValue("@Duration", duration);

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

                string query = @"DELETE FROM [dbo].[PrescriptionItems] WHERE PrescriptionItemID = @PrescriptionItemID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PrescriptionItemID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<bool> CreatePrescriptionItemsAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
        {
            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO [dbo].[PrescriptionItems]
                    ([VisitID]
                    ,[MedicineID]
                    ,[Dosage]
                    ,[Frequency]
                    ,[Duration])
                    VALUES ";

                using (SqlCommand command = new SqlCommand())
                {
                    List<string> valueRows = new List<string>();

                    for (int i = 0; i < items.Count; i++)
                    {
                        string visitParam = $"@VisitID{i}";
                        string medParam = $"@MedicineID{i}";
                        string dosageParam = $"@Dosage{i}";
                        string freqParam = $"@Frequency{i}";
                        string durParam = $"@Duration{i}";

                        valueRows.Add($"({visitParam}, {medParam}, {dosageParam}, {freqParam}, {durParam})");

                        command.Parameters.AddWithValue(visitParam, items[i].visitID);
                        command.Parameters.AddWithValue(medParam, items[i].medicineID);
                        command.Parameters.AddWithValue(dosageParam, items[i].dosage);
                        command.Parameters.AddWithValue(freqParam, items[i].frequency);
                        command.Parameters.AddWithValue(durParam, items[i].duration);
                    }

                    query += string.Join(",\n", valueRows);

                    command.CommandText = query;
                    command.Connection = connection;

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }

        public static async Task<int> UpdatePrescriptionItemsAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
        {
            if (items.Count == 0)
                return 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand())
                {
                    List<string> valueRows = new List<string>();

                    for (int i = 0; i < items.Count; i++)
                    {
                        valueRows.Add($"(@VisitID{i}, @MedicineID{i}, @Dosage{i}, @Frequency{i}, @Duration{i})");

                        command.Parameters.AddWithValue($"@VisitID{i}", items[i].visitID);
                        command.Parameters.AddWithValue($"@MedicineID{i}", items[i].medicineID);
                        command.Parameters.AddWithValue($"@Dosage{i}", items[i].dosage);
                        command.Parameters.AddWithValue($"@Frequency{i}", items[i].frequency);
                        command.Parameters.AddWithValue($"@Duration{i}", items[i].duration);
                    }

                    // Every item belongs to the same visit; this scopes the target so the
                    // DELETE branch can only ever reach this prescription's rows.
                    command.Parameters.AddWithValue("@VisitID", items[0].visitID);

                    command.CommandText = $@"WITH tgt AS (
                        SELECT [VisitID], [MedicineID], [Dosage], [Frequency], [Duration]
                        FROM [dbo].[PrescriptionItems] WITH (HOLDLOCK)
                        WHERE [VisitID] = @VisitID
                    )
                    MERGE INTO tgt
                    USING (VALUES {string.Join(",\n", valueRows)})
                        AS src ([VisitID], [MedicineID], [Dosage], [Frequency], [Duration])
                    ON  tgt.[VisitID] = src.[VisitID]
                    AND tgt.[MedicineID] = src.[MedicineID]
                    WHEN MATCHED THEN
                        UPDATE SET tgt.[Dosage]    = src.[Dosage],
                                   tgt.[Frequency] = src.[Frequency],
                                   tgt.[Duration]  = src.[Duration]
                    WHEN NOT MATCHED BY TARGET THEN
                        INSERT ([VisitID], [MedicineID], [Dosage], [Frequency], [Duration])
                        VALUES (src.[VisitID], src.[MedicineID], src.[Dosage], src.[Frequency], src.[Duration])
                    WHEN NOT MATCHED BY SOURCE THEN
                        DELETE;";

                    command.Connection = connection;

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
