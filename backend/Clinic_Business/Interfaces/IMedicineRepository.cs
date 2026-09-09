using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IMedicineRepository
    {
        Task<clsMedicine?> FindByIdAsync(int id);
        Task<List<clsMedicine>> FindAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteAsync(int id);
        Task<bool> AreValidMedicinesAsync(List<string> medicineIds);
    }
}
