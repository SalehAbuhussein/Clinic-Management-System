using Clinic_Management_System.DTOs.Models.People;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Patients
{
    public class UpdatePatientDTO : UpdatePersonDTO
    {
        [MaxLength(30)]
        public string? EmergencyContactNumber { get; set; }
        [MaxLength(50)]
        public string? EmergencyContactName { get; set; }
    }
}
