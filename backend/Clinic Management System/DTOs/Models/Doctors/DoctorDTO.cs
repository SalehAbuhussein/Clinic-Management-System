using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Doctors
{
    public class DoctorDTO
    {
        public int? Id { get; set; }
        public string Specialization { get; set; }
        public string MedicalSchool { get; set; }
        public ushort GraduationYear { get; set; }
        public int PersonID { get; set; }
        public bool Active { get; set; }
    }
}
