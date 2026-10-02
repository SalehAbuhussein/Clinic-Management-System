using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IMedicineRepository
    {
        Task<clsMedicine?> FindByIdAsync(int id);
        Task<PagedResult<clsMedicine>> FindAllAsync(string? name = null, int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteAsync(int id);
        Task<bool> AreValidMedicinesAsync(List<string> medicineIds);
    }
}
