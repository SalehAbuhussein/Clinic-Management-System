using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.DTOs.Models.Medicines
{
    public class MedicineDTO
    {
        public int? Id { get; set; }
        public int RxCUI { get; set; }
        public string Genericname { get; set; }
        public string Brandname { get; set; }
        public string Form { get; set; }
        public string Strength { get; set; }
    }
}
