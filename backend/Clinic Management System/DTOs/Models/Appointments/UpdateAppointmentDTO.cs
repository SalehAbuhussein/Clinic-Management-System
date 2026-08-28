using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Appointments
{
    public class UpdateAppointmentDTO
    {
        [Required]
        public DateTimeOffset StartAt { get; set; }
        [Required]
        public DateTimeOffset EndAt { get; set; }
        [MaxLength(255)]
        public string? Notes { get; set; }
    }
}
