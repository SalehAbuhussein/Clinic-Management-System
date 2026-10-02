using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        public Task<clsMedicine?> FindByIdAsync(int id)
        {
            return clsMedicine.FindAsync(id);
        }

        public Task<PagedResult<clsMedicine>> FindAllAsync(string? name = null, int pageNumber = 1, int pageSize = 10)
        {
            return clsMedicine.FindAllAsync(name, pageNumber, pageSize);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return clsMedicine.DeleteAsync(id);
        }

        public Task<bool> AreValidMedicinesAsync(List<string> medicineIds)
        {
            return clsMedicine.AreValidMedicinesAsync(medicineIds);
        }
    }
}
