using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IVisitRepository
    {
        Task<clsVisit?> FindByIdAsync(int? id);
        Task<clsVisit?> FindByAppointmentIdAsync(int? id);
        Task<PagedResult<clsVisit.VisitItem>> FindAllAsync(int? doctorId = null, DateOnly? day = null, bool openOnly = false, int pageNumber = 1, int pageSize = 10);
        Task<bool> ExistsByAppointmentIdAsync(int id);
        Task<bool> DeleteAsync(int? id);
        Task<bool> HasCheckedOutAsync(int? id);
    }
}
