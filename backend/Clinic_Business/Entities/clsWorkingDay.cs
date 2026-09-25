using Clinic_Business.Common;
using Clinic_DataAccess;
using System.Data;

namespace Clinic_Business.Entities
{
    public class clsWorkingDay
    {
        public record WorkingDayItem(int Id, string WorkingDay, TimeOnly? StartTime, TimeOnly? EndTime, bool IsWorkingDay);

        public static async Task<PagedResult<WorkingDayItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            DataTable table = await clsWorkingDaysDA.FindAllAsync(pageNumber, pageSize);

            List<WorkingDayItem> workingDays = table.AsEnumerable().Select(row => new WorkingDayItem(
                row.Field<int>("WorkingDayID"),
                row.Field<string>("WorkingDay") ?? string.Empty,
                ToTimeOnly(row.Field<TimeSpan?>("StartTime")),
                ToTimeOnly(row.Field<TimeSpan?>("EndTime")),
                row.Field<bool>("IsWorkingDay")
            )).ToList();

            int total = table.Rows.Count == 0 ? 0 : table.Rows[0].Field<int>("TotalCount");

            return new PagedResult<WorkingDayItem>(workingDays, pageNumber, pageSize, total);
        }

        /// <summary>
        /// A SQL `time` column arrives as a TimeSpan, which measures a duration.
        /// TimeOnly is the type that means a time of day.
        /// </summary>
        /// <param name="value">The column value, or null when the day is closed.</param>
        /// <returns>The same value as a time of day.</returns>
        private static TimeOnly? ToTimeOnly(TimeSpan? value) => value is TimeSpan span ? TimeOnly.FromTimeSpan(span) : null;
    }
}
