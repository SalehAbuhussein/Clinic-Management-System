using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        public Task<clsDoctor?> FindByIdAsync(int? id) => clsDoctor.FindAsync(id);

        public Task<clsDoctor?> FindByUserIdAsync(int? userID) => clsDoctor.FindByUserIDAsync(userID);

        public Task<PagedResult<clsDoctor.DoctorItem>> FindAllAsync(string? fullname = null, string? specialization = null, int pageNumber = 1, int pageSize = 10)
            => clsDoctor.FindAllDoctorsAsync(fullname, specialization, pageNumber, pageSize);

        public Task<bool> DeleteAsync(int? id) => clsDoctor.DeleteDoctorAsync(id);

        // Exists AND the linked user is active.
        public Task<bool> ExistsAsync(int? id) => clsDoctor.IsDoctorExistAsync(id);

        public Task<bool> IsDoctorOfVisitAsync(int? visitId, int? userId)
            => clsDoctor.IsDoctorOfVisitAsync(visitId, userId);
    }
}
