using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Auth
{
    public class LogoutRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
