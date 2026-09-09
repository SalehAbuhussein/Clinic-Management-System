using Clinic_Business.Interfaces;

namespace Clinic_Business
{
    public class DoctorRepository : IDoctorRepository
    {
        public Task<clsDoctor?> FindByIdAsync(int? id) => clsDoctor.FindAsync(id);

        public Task<clsDoctor?> FindByUserIdAsync(int? userID) => clsDoctor.FindByUserIDAsync(userID);

        public Task<List<clsDoctor.DoctorItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
            => clsDoctor.FindAllDoctorsAsync(pageNumber, pageSize);

        public Task<bool> DeleteAsync(int? id) => clsDoctor.DeleteDoctorAsync(id);

        // Exists AND the linked user is active.
        public Task<bool> ExistsAsync(int? id) => clsDoctor.IsDoctorExistAsync(id);

        public Task<bool> IsDoctorOfVisitAsync(int? visitId, int? userId)
            => clsDoctor.IsDoctorOfVisitAsync(visitId, userId);
    }
}
