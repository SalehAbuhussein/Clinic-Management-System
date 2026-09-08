using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Options
{
    public class JwtOptions
    {
        [Required]
        [MinLength(32)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = "ClinicApi";

        [Required]
        public string Audience { get; set; } = "ClinicApiUsers";

        [Range(1, 1440)]
        public int AccessTokenMinutes { get; set; } = 30;

        [Range(1, 90)]
        public int RefreshTokenDays { get; set; } = 7;
    }
}
