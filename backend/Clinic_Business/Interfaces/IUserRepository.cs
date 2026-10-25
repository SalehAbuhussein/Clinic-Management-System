
using Clinic_Business.Common;
using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IUserRepository
    {
        Task<clsUser?> FindByEmailAsync(string email);
        Task<clsUser?> FindByPersonIDAsync(int? personID);
        Task<clsUser?> FindByIdAsync(int? userId);
        Task<PagedResult<clsUser.UserItem>> FindAllAsync(string? fullname = null, string? email = null, bool? active = null, int pageNumber = 1, int pageSize = 10);
        Task<bool> DeactivateAsync(int? userId, int? actorUserId);
        Task<bool> ActivateAsync(int? userId, int? actorUserId);
    }
}
