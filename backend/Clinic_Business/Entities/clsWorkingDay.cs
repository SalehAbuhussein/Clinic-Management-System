using Clinic_Business.Common;
using Clinic_DataAccess;
using System.Data;

namespace Clinic_Business.Entities
{
    public class clsWorkingDay
    {
        public record WorkingDayItem(int Id, string WorkingDay, TimeOnly? StartTime, TimeOnly? EndTime, bool IsWorkingDay);

        /// <summary>
        /// Whether the clinic is open for the whole of an appointment window.
        ///
        /// Times are read as the wall clock the caller sent, because that is
        /// what the clinic's hours are expressed in. A window crossing midnight
        /// cannot fit any single day, so it never opens.
        /// </summary>
        /// <param name="startAt">When the appointment starts.</param>
        /// <param name="endAt">When it ends.</param>
        public static async Task<bool> IsOpenAtAsync(DateTimeOffset startAt, DateTimeOffset endAt)
        {
            if (startAt.DateTime.Date != endAt.DateTime.Date)
                return false;

            DataRow? row = await clsWorkingDaysDA.FindByWeekdayAsync(startAt.DateTime.DayOfWeek.ToString());

            if (row == null)
                return false;

            WorkingDayItem day = MapWorkingDay(row);

            if (!day.IsWorkingDay || day.StartTime is null || day.EndTime is null)
                return false;

            return TimeOnly.FromDateTime(startAt.DateTime) >= day.StartTime
                && TimeOnly.FromDateTime(endAt.DateTime) <= day.EndTime;
        }

        public static async Task<PagedResult<WorkingDayItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            DataTable table = await clsWorkingDaysDA.FindAllAsync(pageNumber, pageSize);

            List<WorkingDayItem> workingDays = table.AsEnumerable().Select(MapWorkingDay).ToList();

            int total = table.Rows.Count == 0 ? 0 : table.Rows[0].Field<int>("TotalCount");

            return new PagedResult<WorkingDayItem>(workingDays, pageNumber, pageSize, total);
        }

        internal static WorkingDayItem MapWorkingDay(DataRow row)
        {
            return new WorkingDayItem(
                row.Field<int>("WorkingDayID"),
                row.Field<string>("WorkingDay") ?? string.Empty,
                ToTimeOnly(row.Field<TimeSpan?>("StartTime")),
                ToTimeOnly(row.Field<TimeSpan?>("EndTime")),
                row.Field<bool>("IsWorkingDay")
            );
        }

        internal static TimeOnly? ToTimeOnly(TimeSpan? value) => value is TimeSpan span ? TimeOnly.FromTimeSpan(span) : null;
    }
}
