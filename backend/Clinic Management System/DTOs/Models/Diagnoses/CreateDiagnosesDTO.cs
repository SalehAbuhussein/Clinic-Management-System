using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Diagnoses
{
    public class CreateDiagnosesDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int VisitID { get; set; }
        [Required]
        [MaxLength(255)]
        public string Diagnosis { get; set; } = string.Empty;
        [MaxLength(255)]
        public string? Notes { get; set; }
    }
}
