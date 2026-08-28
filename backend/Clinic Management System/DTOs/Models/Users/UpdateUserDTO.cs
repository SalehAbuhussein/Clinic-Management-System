using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Users
{
    public class UpdateUserDTO
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }
        [MaxLength(10)]
        [AllowedValues(null, "Admin", "Doctor", "Staff")]
        public string? Role { get; set; }
        public bool? Active { get; set; }
    }
}
