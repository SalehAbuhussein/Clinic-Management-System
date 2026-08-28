using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Doctors
{
    public class UpdateDoctorDTO : UpdatePersonDTO
    {
        [Required]
        [MaxLength(30)]
        public string Specialization { get; set; }
        [Required]
        [MaxLength(150)]
        public string MedicalSchool { get; set; }
        [Required]
        [Range(1900, 2100)]
        public ushort GraduationYear { get; set; }
    }
}
