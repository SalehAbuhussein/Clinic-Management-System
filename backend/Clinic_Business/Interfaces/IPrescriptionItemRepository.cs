using Clinic_Business.Common;
using Clinic_Business.Entities;
using static Clinic_Business.Entities.clsPrescriptionItem;

namespace Clinic_Business.Interfaces
{
    public interface IPrescriptionItemRepository
    {
        Task<PagedResult<PrescriptionItem>> FindAllByVisitIdAsync(int visitId);
        Task<bool> HasPrescriptionByVisitIdAsync(int? visitId);
        Task<bool> CreateManyAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items);
        Task<int> UpdateManyAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items);
    }
}
