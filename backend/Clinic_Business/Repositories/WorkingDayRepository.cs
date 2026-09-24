using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class WorkingDayRepository : IWorkingDayRepository
    {
        public Task<PagedResult<clsWorkingDay.WorkingDayItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10) => clsWorkingDay.FindAllAsync(pageNumber, pageSize);
    }
}
