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
        public static bool Find(int? id, ref int visitID, ref string diagnosis, ref string? notes, ref DateTime createdAt, ref DateTime updatedAt)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT [DiagnosesID]
                    ,[VisitID]
                    ,[Diagnosis]
                    ,[Notes]
                    ,[CreatedAt]
                    ,[UpdatedAt]
                FROM [ClinicManagementSystem].[dbo].[Diagnoses]
                WHERE DiagnosesID = @DiagnosesID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiagnosesID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            visitID = (int)reader["VisitID"];
                            diagnosis = reader["Diagnosis"].ToString() ?? string.Empty;
                            notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString();
                            createdAt = (DateTime)reader["CreatedAt"];
                            updatedAt = (DateTime)reader["UpdatedAt"];
                        }
                    }
                }
            }

            return isFound;
        }
    
        public static bool FindByVisitID(int? visitID, ref int diagnosisID, ref string diagnosis, ref string? notes, ref DateTime createdAt, ref DateTime updatedAt)
        {
            if (!visitID.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT [DiagnosesID]
                    ,[VisitID]
                    ,[Diagnosis]
                    ,[Notes]
                    ,[CreatedAt]
                    ,[UpdatedAt]
                FROM [ClinicManagementSystem].[dbo].[Diagnoses]
                WHERE VisitID = @VisitID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;

                            diagnosisID = (int)reader["DiagnosesID"];
                            diagnosis = reader["Diagnosis"].ToString() ?? string.Empty;
                            notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString();
                            createdAt = (DateTime)reader["CreatedAt"];
                            updatedAt = (DateTime)reader["UpdatedAt"];
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool IsExistByVisitID(int? visitID)
        {
            if (!visitID.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT true=1
                FROM [ClinicManagementSystem].[dbo].[Diagnoses]
                WHERE VisitID = @VisitID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", visitID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static int? Create(int? visitID, string diagnosis, string? notes)
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

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

                    object result = command.ExecuteScalar();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static bool Update(int? id, string diagnosis, string? notes)
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Diagnoses]
                SET [Diagnosis] = @Diagnosis
                    ,[Notes] = @Notes
                WHERE DiagnosesID = @DiagnosesID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiagnosesID", id);
                    command.Parameters.AddWithValue("@Diagnosis", diagnosis);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);

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

                string query = @"DELETE FROM [dbo].[Diagnoses] WHERE DiagnosesID = @DiagnosesID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiagnosesID", id);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }
    }
}
