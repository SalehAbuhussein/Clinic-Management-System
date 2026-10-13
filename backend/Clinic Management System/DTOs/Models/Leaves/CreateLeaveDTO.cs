using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Leaves
{
    public class CreateLeaveDTO
    {
        [Required]
        public DateTimeOffset From { get; set; }
        [Required]
        public DateTimeOffset To { get; set; }
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
