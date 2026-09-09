namespace Clinic_Business.Interfaces
{
    public interface IDoctorRepository
    {
        Task<clsDoctor?> FindByIdAsync(int? id);
        Task<clsDoctor?> FindByUserIdAsync(int? userID);
        Task<List<clsDoctor.DoctorItem>> FindAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteAsync(int? id);
        Task<bool> ExistsAsync(int? id);
        Task<bool> IsDoctorOfVisitAsync(int? visitId, int? userId);
    }
}
