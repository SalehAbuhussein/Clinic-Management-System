using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class VisitRepository : IVisitRepository
    {
        public Task<clsVisit?> FindByIdAsync(int? id) => clsVisit.FindAsync(id);

        public Task<clsVisit?> FindByAppointmentIdAsync(int? id) => clsVisit.FindByAppointmentIDAsync(id);

        public Task<List<clsVisit>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
            => clsVisit.FindAllAsync(pageNumber, pageSize);

        public Task<bool> ExistsByAppointmentIdAsync(int id) => clsVisit.IsExistByAppointmentIDAsync(id);

        public Task<bool> DeleteAsync(int? id) => clsVisit.DeleteAsync(id);

        public Task<bool> HasCheckedOutAsync(int? id) => clsVisit.HasCheckedoutAsync(id);
    }
}
