using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.People
{
    public class PersonDTO
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Midname { get; set; }
        public string? Thirdname { get; set; }
        public string Lastname { get; set; }
        public string Phone { get; set; }
        public bool Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? ProfileImage { get; set; }
    }
}
