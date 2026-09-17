using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        public Task<clsAppointment?> FindByIdAsync(int id) => clsAppointment.FindAsync(id);

        public Task<PagedResult<clsAppointment>> FindAllAsync(
            int? doctorId,
            int? patientId,
            DateOnly? from,
            DateOnly? to,
            int pageNumber = 1,
            int pageSize = 10)
            => clsAppointment.FindAllAsync(doctorId, patientId, from, to, pageNumber, pageSize);

        public Task<bool> ExistsAsync(int? id) => clsAppointment.IsExistByIDAsync(id);

        public Task<bool> DeleteAsync(int id) => clsAppointment.DeleteAsync(id);

        public Task<bool> IsCancelledAsync(int? id) => clsAppointment.IsAppointmentCancelledAsync(id);

        public Task<bool> HasDoctorConflictAsync(int? doctorId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
            => clsAppointment.HasDoctorExistingAppointmentAsync(doctorId, startAt, endAt, excludeAppointmentID);

        public Task<bool> HasPatientConflictAsync(int patientId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
            => clsAppointment.HasPatientExistingAppointmentAsync(patientId, startAt, endAt, excludeAppointmentID);
    }
}
