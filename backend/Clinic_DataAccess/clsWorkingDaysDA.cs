using Microsoft.Data.SqlClient;
using System.Data;

namespace Clinic_DataAccess
{
    public class clsWorkingDaysDA
    {
        /// <summary>
        /// The clinic's hours for one weekday, named as the column stores it.
        /// </summary>
        /// <param name="weekday">Sunday through Saturday.</param>
        public static async Task<DataRow?> FindByWeekdayAsync(string weekday)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT *
                    FROM WorkingDays
                    WHERE WorkingDay = @WorkingDay";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WorkingDay", weekday);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            dt.Load(reader);
                    }
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static async Task<DataTable> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    *,
                    COUNT(*) OVER() AS TotalCount
                    FROM WorkingDays
                    ORDER BY WorkingDayID
                    OFFSET (@PageNumber - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
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
    }
}
