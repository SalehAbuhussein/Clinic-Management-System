using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Doctors
{
    public class DoctorDTO
    {
        public int? Id { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string MedicalSchool { get; set; } = string.Empty;
        public ushort GraduationYear { get; set; }
        public int PersonID { get; set; }
        public bool Active { get; set; }
    }
}
