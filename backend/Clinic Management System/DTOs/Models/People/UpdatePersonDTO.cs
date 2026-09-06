using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.People
{
    public class UpdatePersonDTO
    {
        [Required]
        [MaxLength(20)]
        public string Firstname { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string Midname { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? Thirdname { get; set; }
        [Required]
        [MaxLength(20)]
        public string Lastname { get; set; } = string.Empty;
        [Required]
        [MaxLength(15)]
        public string Phone { get; set; } = string.Empty;
        [Required]
        public bool Gender { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [MaxLength(255)]
        public string? ProfileImage { get; set; }
    }
}
