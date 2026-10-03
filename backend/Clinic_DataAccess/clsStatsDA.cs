using Microsoft.Data.SqlClient;
using System.Data;

namespace Clinic_DataAccess
{
    public class clsStatsDA
    {
        public record TotalsRow(int InClinic, int AppointmentsToday, int AttendedToday, int NoShowToday, int Patients);

        /// <summary>
        /// The tiles. Every count is a scalar subquery in one statement rather than
        /// five commands.
        /// </summary>
        /// <param name="todayFrom">Midnight of the caller's today.</param>
        /// <param name="todayTo">The midnight after it.</param>
        public static async Task<TotalsRow> FindTotalsAsync(DateTimeOffset todayFrom, DateTimeOffset todayTo)
        {
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT
                    (SELECT COUNT(*) FROM Visits WHERE CheckoutTime IS NULL) AS InClinic,
                    (SELECT COUNT(*) FROM Appointments WHERE StartAt >= @From AND StartAt < @To) AS AppointmentsToday,
                    (SELECT COUNT(*) FROM Appointments WHERE StartAt >= @From AND StartAt < @To AND [Status] = 1) AS AttendedToday,
                    (SELECT COUNT(*) FROM Appointments WHERE StartAt >= @From AND StartAt < @To AND [Status] = 2) AS NoShowToday,
                    (SELECT COUNT(*) FROM Patients) AS Patients;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@From", todayFrom);
                    command.Parameters.AddWithValue("@To", todayTo);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            return new TotalsRow(0, 0, 0, 0, 0);

                        return new TotalsRow(
                            (int)reader["InClinic"],
                            (int)reader["AppointmentsToday"],
                            (int)reader["AttendedToday"],
                            (int)reader["NoShowToday"],
                            (int)reader["Patients"]
                        );
                    }
                }
            }
        }

        /// <summary>
        /// How many appointments fall in each day of the range, as (index, count)
        /// pairs counted from the caller's own midnight. Days with none are absent.
        ///
        /// ponytail: buckets are whole 24-hour blocks from @From, so a daylight-saving
        /// day would shift an hour. Group with AT TIME ZONE if a clinic ever observes
        /// one, and accept that it stops being an index seek.
        /// </summary>
        public static async Task<Dictionary<int, int>> FindAppointmentsByDayAsync(DateTimeOffset from, DateTimeOffset to)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT DATEDIFF(hour, @From, StartAt) / 24 AS DayIndex, COUNT(*) AS Total
                FROM Appointments
                WHERE StartAt >= @From AND StartAt < @To
                GROUP BY DATEDIFF(hour, @From, StartAt) / 24
                ORDER BY DayIndex;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@From", from);
                    command.Parameters.AddWithValue("@To", to);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            counts[(int)reader["DayIndex"]] = (int)reader["Total"];
                    }
                }
            }

            return counts;
        }
    }
}
