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
    public class AuditRepository : IAuditRepository
    {
        public Task<int> GetTotalCountAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default) => clsAuditEntry.GetTotalCountAsync(from, to, cancellationToken);

        public Task<PagedResult<clsAuditEntry.AuditEntryItem>> FindAllAsync(DateTimeOffset? from, DateTimeOffset? to, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default) => clsAuditEntry.FindAllAsync(from, to, pageNumber, pageSize, cancellationToken);
        public Task<bool> CreateAsync(int? actorUserId, string action, string? targetType = null, int? targetId = null, string? detail = null) => clsAuditEntry.CreateAsync(actorUserId, action, targetType, targetId, detail);
    }
}
