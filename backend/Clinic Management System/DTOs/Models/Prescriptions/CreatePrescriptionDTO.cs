using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Prescriptions
{
    public class CreatePrescriptionDTO : IValidatableObject
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int VisitID { get; set; }
        [Required]
        [MinLength(1)]
        public List<CreatePrescriptionItemDTO> Items { get; set; } = new List<CreatePrescriptionItemDTO>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<int> duplicates = Items
                .GroupBy(item => item.MedicineID)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                yield return new ValidationResult(
                    $"Medicine {string.Join(", ", duplicates)} appears more than once. Each medicine may appear only once in a prescription.",
                    new[] { nameof(Items) }
                );
            }
        }
    }

    public class CreatePrescriptionItemDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int MedicineID { get; set; }
        [Required, MaxLength(50)]
        public string Dosage { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string Frequency { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string Duration { get; set; } = string.Empty;
    }
}
