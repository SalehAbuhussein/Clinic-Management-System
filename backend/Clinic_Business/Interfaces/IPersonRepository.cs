using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IPersonRepository
    {
        Task<clsPerson?> FindByIdAsync(int? id);
        Task<PagedResult<clsPerson>> FindAllAsync(string? fullname = null, string? nationalNo = null, string? phone = null, int pageNumber = 1, int pageSize = 10);
        Task<bool> UpdateAsync(clsPerson person);
        Task<bool> IsUsedAsync(int id);
        Task<bool> DeleteAsync(int? id);
    }
}
