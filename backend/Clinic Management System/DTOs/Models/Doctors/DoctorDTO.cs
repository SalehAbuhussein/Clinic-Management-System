using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Doctors
{
    public class DoctorDTO
    {
        public int? Id { get; set; }
        public string? Fullname { get; set; }
        public string Firstname { get; set; } = string.Empty;
        public string Midname { get; set; } = string.Empty;
        public string? Thirdname { get; set; }
        public string Lastname { get; set; } = string.Empty;
        public string NationalNo { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string MedicalSchool { get; set; } = string.Empty;
        public ushort GraduationYear { get; set; }
        public int PersonID { get; set; }
        public bool Active { get; set; }
        public string? ProfileImage { get; set; }
    }
}
