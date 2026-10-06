namespace Clinic_Management_System.DTOs.Models.Leaves
{
    public class UpdateLeaveDTO
    {
        public string Reason { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }
}
