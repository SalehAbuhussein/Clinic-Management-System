using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Medicines
{
    public class CreateMedicineDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int RxCUI { get; set; }
        [Required]
        [MaxLength(3000)]
        public string Genericname { get; set; }
        [MaxLength(255)]
        public string Brandname { get; set; }
        [Required]
        [MaxLength(100)]
        public string Form { get; set; }
        [Required]
        [MaxLength(255)]
        public string Strength { get; set; }
    }
}
