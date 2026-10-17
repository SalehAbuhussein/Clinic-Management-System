using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IWorkingDayRepository
    {
        Task<PagedResult<clsWorkingDay.WorkingDayItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> IsOpenAtAsync(DateTimeOffset startAt, DateTimeOffset endAt);
    }
}
