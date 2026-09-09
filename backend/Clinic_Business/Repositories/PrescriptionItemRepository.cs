using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class PrescriptionItemRepository : IPrescriptionItemRepository
    {
        public Task<List<clsPrescriptionItem>> FindAllByVisitIdAsync(int visitId, int pageNumber = 1, int pageSize = 10)
            => clsPrescriptionItem.FindAllByVisitIDAsync(visitId, pageNumber, pageSize);

        public Task<bool> HasPrescriptionByVisitIdAsync(int? visitId)
            => clsPrescriptionItem.HasPrescriptionByVisitIDAsync(visitId);

        public Task<bool> CreateManyAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
            => clsPrescriptionItem.CreatePrescriptionItemsAsync(items);

        public Task<int> UpdateManyAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
            => clsPrescriptionItem.UpdatePrescriptionItemsAsync(items);
    }
}
