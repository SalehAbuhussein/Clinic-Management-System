using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        public Task<clsAppointment?> FindByIdAsync(int id) => clsAppointment.FindAsync(id);

        public Task<int> GetTotalCountAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default) => clsAppointment.GetTotalCountAsync(doctorId, patientId, from, to, cancellationToken);

        public Task<PagedResult<clsAppointment>> FindAllAsync(
            int? doctorId,
            int? patientId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default
        )
            => clsAppointment.FindAllAsync(doctorId, patientId, from, to, pageNumber, pageSize, cancellationToken);

        public Task<bool> ExistsAsync(int? id) => clsAppointment.IsExistByIDAsync(id);

        public Task<bool> DeleteAsync(int id) => clsAppointment.DeleteAsync(id);

        public Task<bool> IsCancelledAsync(int? id) => clsAppointment.IsAppointmentCancelledAsync(id);

        public Task<bool> HasDoctorConflictAsync(int? doctorId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null)
            => clsAppointment.HasDoctorExistingAppointmentAsync(doctorId, startAt, endAt, excludeAppointmentID);

        public Task<bool> HasPatientConflictAsync(int patientId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null)
            => clsAppointment.HasPatientExistingAppointmentAsync(patientId, startAt, endAt, excludeAppointmentID);
    }
}
