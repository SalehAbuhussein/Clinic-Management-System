using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsDiagnosisDA
    {
        public static async Task<DataRow?> FindAsync(int? id)
        {
            if (!id.HasValue)
                return null;

            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT D.[DiagnosesID]
                    ,D.[Diagnosis]
                    ,D.[Notes] AS DiagnosisNotes
                    ,D.[CreatedAt]
                    ,D.[UpdatedAt]

                    ,V.[VisitID]
                    ,V.[AppointmentID]
                    ,V.[CheckinTime]
                    ,V.[CheckoutTime]
                    ,V.[Notes]
                    ,V.[IsLocked]
                FROM [dbo].[Diagnoses] D
                INNER JOIN [dbo].[Visits] V ON V.VisitID = D.VisitID
                WHERE D.DiagnosesID = @Value";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Value", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
    
        public static async Task<DataRow?> FindByVisitIDAsync(int? visitID)
        {
            if (!visitID.HasValue)
                return null;
                
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT D.[DiagnosesID]
                    ,D.[Diagnosis]
                    ,D.[Notes] AS DiagnosisNotes
                    ,D.[CreatedAt]
                    ,D.[UpdatedAt]

                    ,V.[VisitID]
                    ,V.[AppointmentID]
                    ,V.[CheckinTime]
                    ,V.[CheckoutTime]
                    ,V.[Notes]
                    ,V.[IsLocked]
                FROM [dbo].[Diagnoses] D
                INNER JOIN [dbo].[Visits] V ON V.VisitID = D.VisitID
                WHERE D.VisitID = @Value";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Value", visitID);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static async Task<bool> IsExistByVisitIDAsync(int? visitID)
        {
            if (!visitID.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Diagnoses]
                WHERE VisitID = @VisitID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitID);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static async Task<int?> CreateAsync(int? visitID, string diagnosis, string? notes)
        {
            if (!visitID.HasValue)
                return null;

            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO [dbo].[Diagnoses]
                    ([VisitID]
                    ,[Diagnosis]
                    ,[Notes])
                    VALUES
                    (@VisitID
                    ,@Diagnosis
                    ,@Notes); SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitID);
                    command.Parameters.AddWithValue("@Diagnosis", diagnosis);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);

                    object? result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static async Task<bool> UpdateAsync(int? id, string diagnosis, string? notes)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"UPDATE [dbo].[Diagnoses]
                SET [Diagnosis] = @Diagnosis
                    ,[Notes] = @Notes
                WHERE DiagnosesID = @DiagnosesID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiagnosesID", id);
                    command.Parameters.AddWithValue("@Diagnosis", diagnosis);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);

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

                string query = @"DELETE FROM [dbo].[Diagnoses] WHERE DiagnosesID = @DiagnosesID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiagnosesID", id);

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected > 0;
        }
    }
}
