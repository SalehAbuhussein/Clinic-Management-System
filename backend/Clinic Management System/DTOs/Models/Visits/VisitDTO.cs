using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Visits
{
    public class VisitDTO
    {
        [Range(1, int.MaxValue)]
        [Required]
        public int? Id { get; set; }
        [Range(1, int.MaxValue)]
        [Required]
        public int AppointmentID { get; set; }
        [Required]
        public DateTimeOffset CheckinTime { get; set; }
        public DateTimeOffset? CheckoutTime { get; set; }
        public string? Notes { get; set; }
    }
}
