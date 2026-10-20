using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IPatientRepository
    {
        Task<clsPatient?> FindByIdAsync(int? id);
        Task<clsPatient?> FindByNationalNoAsync(string nationalNo);
        Task<PagedResult<clsPatient.PatientItem>> FindAllAsync(string? fullname = null, string? phone = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteAsync(int? id);
        Task<bool> ExistsAsync(int? id);
    }
}
