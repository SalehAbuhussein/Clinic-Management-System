namespace Clinic_Management_System.DTOs.Models.Prescriptions
{
    public class PrescriptionDTO
    {
        public int Id { get; set; }
        public int VisitID { get; set; }
        public int MedicineID { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }
}
