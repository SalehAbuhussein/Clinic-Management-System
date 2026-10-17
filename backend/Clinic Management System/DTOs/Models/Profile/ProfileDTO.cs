namespace Clinic_Management_System.DTOs.Models.Profile
{
    /// <summary>
    /// The signed-in user's own record. It carries the account alongside the
    /// person, because a profile screen shows both and the token names neither.
    /// </summary>
    public class ProfileDTO
    {
        public int? UserId { get; set; }
        public int? PersonId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Firstname { get; set; } = string.Empty;
        public string Midname { get; set; } = string.Empty;
        public string? Thirdname { get; set; }
        public string Lastname { get; set; } = string.Empty;
        public string NationalNo { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? ProfileImage { get; set; }
    }
}
