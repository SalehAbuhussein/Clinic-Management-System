using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Patients
{
    public class PatientDTO
    {
        public int? Id { get; set; }
        public string? Fullname { get; set; }
        public string? Firstname { get; set; }
        public string? Midname { get; set; }
        public string? Thirdname { get; set; }
        public string? Lastname { get; set; }
        public string? NationalNo { get; set; }
        public bool? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public int? PersonID { get; set; }
    }
}
