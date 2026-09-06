using Clinic_Management_System.DTOs.Models.People;

namespace Clinic_Management_System.DTOs.Models.Users
{
    public class UserDTO
    {
        public int? Id { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
