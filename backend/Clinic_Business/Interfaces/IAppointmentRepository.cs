using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<clsAppointment?> FindByIdAsync(int id);
        Task<PagedResult<clsAppointment>> FindAllAsync(
            int? doctorId,
            int? patientId,
            DateOnly? from,
            DateOnly? to,
            int pageNumber = 1,
            int pageSize = 10);
        Task<bool> ExistsAsync(int? id);
        Task<bool> DeleteAsync(int id);
        Task<bool> IsCancelledAsync(int? id);
        Task<bool> HasDoctorConflictAsync(int? doctorId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null);
        Task<bool> HasPatientConflictAsync(int patientId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null);
    }
}
