using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.People
{
    public class PersonDTO
    {
        public int Id { get; set; }
        public string Firstname { get; set; } = string.Empty;
        public string Midname { get; set; } = string.Empty;
        public string? Thirdname { get; set; }
        public string Lastname { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? ProfileImage { get; set; }
    }
}
