using Clinic_Business.Common;
using Clinic_Business.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Interfaces
{
    public interface IAuditRepository
    {
        Task<PagedResult<clsAuditEntry.AuditEntryItem>> FindAllAsync(DateTimeOffset? from, DateTimeOffset? to, int pageNumber = 1, int pageSize = 10);
        Task<bool> CreateAsync(int? actorUserId, string action, string? targetType = null, int? targetId = null, string? detail = null);
    }
}
