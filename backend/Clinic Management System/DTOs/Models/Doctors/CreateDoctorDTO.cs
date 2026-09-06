using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Doctors
{
    public class CreateDoctorDTO : CreatePersonDTO
    {
        [Required]
        [MaxLength(30)]
        public string Specialization { get; set; } = string.Empty;
        [Required]
        [MaxLength(150)]
        public string MedicalSchool { get; set; } = string.Empty;
        [Required]
        [Range(1900, 2100)]
        public ushort GraduationYear { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(255)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
