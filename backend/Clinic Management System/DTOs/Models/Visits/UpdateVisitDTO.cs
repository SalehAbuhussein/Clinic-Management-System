using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Visits
{
    public class UpdateVisitDTO
    {
        public DateTimeOffset? CheckinTime { get; set; }
        [MaxLength(255)]
        public string? Notes { get; set; }
    }
}
