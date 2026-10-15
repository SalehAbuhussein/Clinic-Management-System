using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Auth
{
    public class UpdatePasswordRequest
    {
        /// <summary>
        /// Required only when changing your own password; an admin resetting
        /// someone else's cannot know it.
        /// </summary>
        public string? CurrentPassword { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
