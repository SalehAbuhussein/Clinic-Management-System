namespace Clinic_Management_System.DTOs.Auth
{
    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
