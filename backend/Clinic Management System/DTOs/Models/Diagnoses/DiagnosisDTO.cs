namespace Clinic_Management_System.DTOs.Models.Diagnoses
{
    public class DiagnosisDTO
    {
        public int DiagnosisID { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
