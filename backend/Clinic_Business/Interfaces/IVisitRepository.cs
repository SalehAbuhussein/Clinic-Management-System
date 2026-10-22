using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IVisitRepository
    {
        Task<clsVisit?> FindByIdAsync(int? id);
        Task<clsVisit?> FindByAppointmentIdAsync(int? id);
        Task<int> GetTotalCountAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);

        Task<PagedResult<clsVisit.VisitItem>> FindAllAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
        Task<bool> ExistsByAppointmentIdAsync(int id);
        Task<bool> DeleteAsync(int? id);
        Task<bool> HasCheckedOutAsync(int? id);
    }
}
