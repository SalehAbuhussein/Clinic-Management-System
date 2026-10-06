using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Repositories
{
    public class LeaveRepository : ILeaveRepository
    {
        public async Task<PagedResult<clsLeave.LeaveItem>> FindAllLeavesAsync(DateTimeOffset? from, DateTimeOffset? to, int pageNumber = 1, int pageSize = 10)
        {
            return await clsLeave.FindAllLeavesAsync(from, to, pageNumber, pageSize);
        }

        public async Task<clsLeave?> FindAsync(int id)
        {
            return await clsLeave.FindAsync(id);
        }
    }
}
