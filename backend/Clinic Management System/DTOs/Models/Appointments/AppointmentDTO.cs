using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Appointments
{
    public class AppointmentDTO
    {
        public int Id { get; set; }
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int PatientId { get; set;  }
        public string PatientName { get; set; } = string.Empty;
        public int? VisitId { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public byte Status { get; set; }
        public string? Notes { get; set; }
    }

    public class StatusAttribute : ValidationAttribute
    {
        private readonly byte[] _ValidValues;

        public StatusAttribute(params byte[] validValues)
        {
            _ValidValues = validValues;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is byte status && _ValidValues.Contains(status))
                return ValidationResult.Success;

            return new ValidationResult("Invalid status value.");
        }
    }
}
