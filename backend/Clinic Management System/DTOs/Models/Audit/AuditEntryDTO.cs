namespace Clinic_Management_System.DTOs.Models.Audit
{
    public class AuditEntryDTO
    {
        public int Id { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public int? ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string Detail { get; set; } = string.Empty;
    }
}
