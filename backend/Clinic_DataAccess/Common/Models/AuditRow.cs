namespace Clinic_DataAccess.Common.Models
{
    /// <summary>
    /// One audit entry, carried alongside the change it records so both commit
    /// together.
    /// </summary>
    public record AuditRow(int? ActorUserId, string Action, string? TargetType, int? TargetId, string? Detail = null);
}
