namespace Clinic_Business.Interfaces
{
    public interface IVisitRepository
    {
        Task<clsVisit?> FindByIdAsync(int? id);
        Task<clsVisit?> FindByAppointmentIdAsync(int? id);
        Task<List<clsVisit>> FindAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> ExistsByAppointmentIdAsync(int id);
        Task<bool> DeleteAsync(int? id);
        Task<bool> HasCheckedOutAsync(int? id);
    }
}
