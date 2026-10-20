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
            int? userId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            byte? status = null,
            int pageNumber = 1,
            int pageSize = 10);
        Task<clsLeave?> FindAsync(int id);
        Task<bool> HasDoctorLeaveAsync(int? doctorId, DateTimeOffset startAt, DateTimeOffset endAt);
        Task<bool> HasUserLeaveAsync(int? userId, DateTimeOffset startAt, DateTimeOffset endAt);
    }
}
