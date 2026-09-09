using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IPatientRepository
    {
        Task<clsPatient?> FindByIdAsync(int? id);
        Task<clsPatient?> FindByNationalNoAsync(string nationalNo);
        Task<List<clsPatient.PatientItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteAsync(int? id);
        Task<bool> ExistsAsync(int? id);
    }
}
