using Clinic_Business.Common;
using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Entities
{
    public class clsAuditEntry
    {
        public record AuditEntryItem(
            int Id,
            DateTimeOffset OccurredAt,
            int? ActorUserID,
            string ActorName,
            string Action,
            string TargetType,
            int? TargetID,
            string Detail
        );

        public static Task<int> GetTotalCountAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default) => clsAuditEntryDA.GetTotalCount(from, to, cancellationToken);

        public static async Task<PagedResult<AuditEntryItem>> FindAllAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default
        )
        {
            DataTable dt = await clsAuditEntryDA.FindAllAsync(from, to, pageNumber, pageSize, cancellationToken);

            List<AuditEntryItem> entries = dt.AsEnumerable().Select(row => new AuditEntryItem(
                row.Field<int>("AuditEntryID"),
                row.Field<DateTimeOffset>("OccurredAt"),
                row.Field<int?>("ActorUserID"),
                row.Field<string>("ActorName") ?? string.Empty,
                row.Field<string>("Action") ?? string.Empty,
                row.Field<string>("TargetType") ?? string.Empty,
                row.Field<int?>("TargetID"),
                row.Field<string>("Detail") ?? string.Empty
            )).ToList();

            int total = 0;

            if (dt.Rows.Count > 0)
                total = dt.Rows[0].Field<int>("TotalCount");
            else if (pageNumber > 1)
                total = await clsAuditEntryDA.GetTotalCount(from, to, cancellationToken);

            return new PagedResult<AuditEntryItem>(entries, pageNumber, pageSize, total);
        }

        public static async Task<bool> CreateAsync(
            int? actorUserId,
            string action,
            string? targetType = null,
            int? targetId = null,
            string? detail = null
        )
        {
            int? id = await clsAuditEntryDA.CreateAsync(actorUserId, action, targetType, targetId, detail);

            return id.HasValue;
        }
    }
}
