using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsAuditEntryDA
    {
        public static async Task<DataTable> FindAllAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10
        )
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    COUNT(*) OVER() AS TotalCount,
                    A.AuditEntryID,
                    A.OccurredAt,
                    A.ActorUserID,
                    A.[Action],
                    A.TargetType,
                    A.TargetID,
                    A.Detail,
                    CONCAT_WS(' ', P.Firstname, P.Midname, P.Thirdname, P.Lastname) AS ActorName
                FROM AuditEntries A
                LEFT JOIN Users U ON U.UserID = A.ActorUserID
                LEFT JOIN People P ON P.PersonID = U.PersonID";

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;

                    List<string> conditions = new List<string>();

                    if (from.HasValue)
                    {
                        conditions.Add("A.OccurredAt >= @From");
                        command.Parameters.AddWithValue("@From", from.Value);
                    }

                    if (to.HasValue)
                    {
                        conditions.Add("A.OccurredAt < @To");
                        command.Parameters.AddWithValue("@To", to.Value);
                    }

                    if (conditions.Count > 0)
                        query += $"\nWHERE {string.Join(" AND ", conditions)}";

                    query += @"
                        ORDER BY A.AuditEntryID DESC
                        OFFSET (@PageNumber - 1) * @PageSize ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";

                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    command.CommandText = query;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt;
        }

        public static async Task<int?> CreateAsync(
            int? actorUserId,
            string action,
            string? targetType,
            int? targetId,
            string? detail
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO [dbo].[AuditEntries]
                    ([ActorUserID]
                    ,[Action]
                    ,[TargetType]
                    ,[TargetID]
                    ,[Detail])
                VALUES
                    (@ActorUserID
                    ,@Action
                    ,@TargetType
                    ,@TargetID
                    ,@Detail); SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ActorUserID", (object?)actorUserId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Action", action);
                    command.Parameters.AddWithValue("@TargetType", (object?)targetType ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TargetID", (object?)targetId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Detail", (object?)detail ?? DBNull.Value);

                    object? result = await command.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }
    }
}
