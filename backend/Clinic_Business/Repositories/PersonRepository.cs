using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class PersonRepository : IPersonRepository
    {
        public Task<clsPerson?> FindByIdAsync(int? id) => clsPerson.FindAsync(id);
        public Task<PagedResult<clsPerson>> FindAllAsync(string? fullname = null, string? nationalNo = null, string? phone = null, int pageNumber = 1, int pageSize = 10) => clsPerson.FindAllAsync(fullname, nationalNo, phone, pageNumber, pageSize);
        public Task<bool> UpdateAsync(clsPerson person) => person.UpdateAsync();
        public Task<bool> IsUsedAsync(int id) => clsPerson.IsUsedAsync(id);
        public Task<bool> DeleteAsync(int? id) => clsPerson.DeleteAsync(id);
    }
}
