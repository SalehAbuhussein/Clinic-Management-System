using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Appointments
{
    public class AppointmentDTO
    {
        public int Id { get; set; }
        public int? DoctorID { get; set; }
        public int PatientID { get; set;  }
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
