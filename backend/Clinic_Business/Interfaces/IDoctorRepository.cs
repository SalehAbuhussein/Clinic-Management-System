using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IDoctorRepository
    {
        Task<clsDoctor?> FindByIdAsync(int? id);
        Task<clsDoctor?> FindByUserIdAsync(int? userID);
        Task<PagedResult<clsDoctor.DoctorItem>> FindAllAsync(string? fullname = null, string? specialization = null, bool? active = null, int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteAsync(int? id);
        Task<bool> ExistsAsync(int? id);
        Task<bool> IsDoctorOfVisitAsync(int? visitId, int? userId);
    }
}
