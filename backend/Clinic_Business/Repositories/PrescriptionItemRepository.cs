using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using static Clinic_Business.Entities.clsPrescriptionItem;

namespace Clinic_Business.Repositories
{
    public class PrescriptionItemRepository : IPrescriptionItemRepository
    {
        public Task<PagedResult<PrescriptionItem>> FindAllByVisitIdAsync(int visitId)
            => clsPrescriptionItem.FindAllByVisitIDAsync(visitId);

        public Task<bool> HasPrescriptionByVisitIdAsync(int? visitId)
            => clsPrescriptionItem.HasPrescriptionByVisitIDAsync(visitId);

        public Task<bool> HasPrescriptionByMedicineIdAsync(int medicineId)
            => clsPrescriptionItem.HasPrescriptionByMedicineIDAsync(medicineId);

        public Task<bool> CreateManyAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
            => clsPrescriptionItem.CreatePrescriptionItemsAsync(items);

        public Task<int> UpdateManyAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
            => clsPrescriptionItem.UpdatePrescriptionItemsAsync(items);
    }
}
