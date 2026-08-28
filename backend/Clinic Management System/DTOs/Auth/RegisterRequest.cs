using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Firstname { get; set; }
        public string Midname { get; set; }
        public string? Thirdname { get; set; }
        public string Lastname { get; set; }
        public string NationalNo { get; set; }
        public string Phone { get; set; }
        public bool Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }
}
