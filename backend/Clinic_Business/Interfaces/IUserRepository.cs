
using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IUserRepository
    {
        Task<clsUser?> FindByEmailAsync(string email);
        Task<clsUser?> FindByPersonIDAsync(int? personID);
        Task<clsUser?> FindByIdAsync(int? userId);
        Task<PagedResult<clsUser.UserItem>> FindUsersAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> Deactivate(int? userId);
        Task<bool> Activate(int? userId);
    }
}
