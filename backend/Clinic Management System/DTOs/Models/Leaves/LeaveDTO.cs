namespace Clinic_Management_System.DTOs.Models.Leaves
{
    public class LeaveDTO
    {
        public int? Id { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }
}
