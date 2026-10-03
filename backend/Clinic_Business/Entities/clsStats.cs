using Clinic_DataAccess;

namespace Clinic_Business.Entities
{
    /// <summary>
    /// The admin dashboard's numbers. Read-only and aggregate, so it carries
    /// projection records rather than the active-record shape the entities use.
    /// </summary>
    public class clsStats
    {
        public record ClinicStats(
            int InClinic,
            int AppointmentsToday,
            int AttendedToday,
            int NoShowToday,
            int Patients,
            List<int> AppointmentsByDay);

        /// <summary>
        /// Builds the dashboard from two queries.
        ///
        /// The caller sends its own midnights, the same way every other day-scoped
        /// query in this API works, so nothing here decides where a day starts.
        /// </summary>
        /// <param name="todayFrom">Midnight of the caller's today.</param>
        /// <param name="from">Midnight of the first day of the chart.</param>
        /// <param name="days">How many days the chart covers.</param>
        public static async Task<ClinicStats> FindAsync(DateTimeOffset todayFrom, DateTimeOffset from, int days)
        {
            DateTimeOffset todayTo = todayFrom.AddDays(1);
            DateTimeOffset to = from.AddDays(days);

            clsStatsDA.TotalsRow totals = await clsStatsDA.FindTotalsAsync(todayFrom, todayTo);
            Dictionary<int, int> byDay = await clsStatsDA.FindAppointmentsByDayAsync(from, to);

            // Dense, so the client draws one bar per day without filling gaps itself.
            List<int> series = Enumerable.Range(0, days)
                .Select(index => byDay.TryGetValue(index, out int total) ? total : 0)
                .ToList();

            return new ClinicStats(
                totals.InClinic,
                totals.AppointmentsToday,
                totals.AttendedToday,
                totals.NoShowToday,
                totals.Patients,
                series);
        }
    }
}
