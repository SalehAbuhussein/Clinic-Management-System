using Clinic_Management_System.DTOs.Models.People;

namespace Clinic_Management_System.DTOs.Models.Users
{
    public class UserDTO
    {
        public int? Id { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool Active { get; set; }
    }
}
