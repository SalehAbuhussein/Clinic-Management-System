using Clinic_Business.Common;
using Clinic_DataAccess;
using System.Data;

namespace Clinic_Business.Entities
{
    public class clsWorkingDay
    {
        public record WorkingDayItem(int Id, string WorkingDay, TimeSpan? StartAt, TimeSpan? EndAt, bool IsWorkingDay);

        public static async Task<PagedResult<WorkingDayItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            DataTable table = await clsWorkingDaysDA.FindAllAsync(pageNumber, pageSize);

            List<WorkingDayItem> workingDays = table.AsEnumerable().Select(row => new WorkingDayItem(
                row.Field<int>("WorkingDayID"),
                row.Field<string>("WorkingDay") ?? string.Empty,
                row.Field<TimeSpan?>("StartTime"),
                row.Field<TimeSpan?>("EndTime"),
                row.Field<bool>("IsWorkingDay")
            )).ToList();

            int total = table.Rows.Count == 0 ? 0 : table.Rows[0].Field<int>("TotalCount");

            return new PagedResult<WorkingDayItem>(workingDays, pageNumber, pageSize, total);
        }
    }
}
