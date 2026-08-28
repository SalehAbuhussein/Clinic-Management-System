using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Patients
{
    public class PatientDTO
    {
        public int? Id { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public int? PersonID { get; set; }
    }
}
