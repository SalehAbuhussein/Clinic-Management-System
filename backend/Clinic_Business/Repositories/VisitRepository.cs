using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class VisitRepository : IVisitRepository
    {
        public Task<clsVisit?> FindByIdAsync(int? id) => clsVisit.FindAsync(id);

        public Task<clsVisit?> FindByAppointmentIdAsync(int? id) => clsVisit.FindByAppointmentIDAsync(id);

        public Task<int> GetTotalCountAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null) => clsVisit.GetTotalCountAsync(doctorId, patientId, from, to);

        public Task<PagedResult<clsVisit.VisitItem>> FindAllAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int pageNumber = 1, int pageSize = 10)
            => clsVisit.FindAllAsync(doctorId, patientId, from, to, pageNumber, pageSize);

        public Task<bool> ExistsByAppointmentIdAsync(int id) => clsVisit.IsExistByAppointmentIDAsync(id);

        public Task<bool> DeleteAsync(int? id) => clsVisit.DeleteAsync(id);

        public Task<bool> HasCheckedOutAsync(int? id) => clsVisit.HasCheckedoutAsync(id);
    }
}
