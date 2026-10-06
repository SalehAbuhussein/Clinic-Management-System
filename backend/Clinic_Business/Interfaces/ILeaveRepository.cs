using Clinic_Business.Common;
using Clinic_Business.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Clinic_Business.Entities.clsLeave;

namespace Clinic_Business.Interfaces
{
    public interface ILeaveRepository
    {
        Task<PagedResult<LeaveItem>> FindAllLeavesAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10);
        Task<clsLeave?> FindAsync(int id);
    }
}
