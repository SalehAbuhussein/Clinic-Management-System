using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<clsAppointment?> FindByIdAsync(int id);
        Task<int> GetTotalCountAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null);

        Task<PagedResult<clsAppointment>> FindAllAsync(
            int? doctorId,
            int? patientId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10);
        Task<bool> ExistsAsync(int? id);
        Task<bool> DeleteAsync(int id);
        Task<bool> IsCancelledAsync(int? id);
        Task<bool> HasDoctorConflictAsync(int? doctorId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null);
        Task<bool> HasPatientConflictAsync(int patientId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null);
    }
}
