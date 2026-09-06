using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Auth
{
    // Registration is a person plus credentials. The person half is defined once, in
    // CreatePersonDTO, the same way CreateDoctorDTO and CreatePatientDTO do it — so the
    // name/phone/national-number rules can never drift between registering a user and
    // creating a doctor.
    public class RegisterRequest : CreatePersonDTO
    {
        // [EmailAddress] passes on null and "" like every attribute except [Required],
        // so the two belong together; without [Required] an empty email got through.
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MinLength(6)]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;
    }
}
