using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Appointments
{
    public class CreateAppointmentDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int DoctorID { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int PatientID { get; set; }
        [Required]
        public DateTimeOffset StartAt { get; set; }
        [Required]
        public DateTimeOffset EndAt { get; set; }
        [Status(0, 1, 2, 3)]
        public byte Status { get; set; }
        [MaxLength(255)]
        public string? Notes { get; set; }
    }
}
