using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.People
{
    public class CreatePersonDTO
    {
        [Required]
        [MaxLength(20)]
        public string Firstname { get; set; }
        [Required]
        [MaxLength(20)]
        public string Midname { get; set; }
        [MaxLength(20)]
        public string? Thirdname { get; set; }
        [Required]
        [MaxLength(20)]
        public string Lastname { get; set; }
        [Required]
        [MaxLength(15)]
        public string NationalNo { get; set; }
        [Required]
        [MaxLength(15)]
        public string Phone { get; set; }
        public bool Gender { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [MaxLength(255)]
        public string? ProfileImage { get; set; }
    }
}
