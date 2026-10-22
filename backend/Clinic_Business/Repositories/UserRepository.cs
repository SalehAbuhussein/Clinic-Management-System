using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class UserRepository : IUserRepository
    {
        public Task<clsUser?> FindByEmailAsync(string email)
        {
            return clsUser.FindByEmailAsync(email);
        }

        public Task<clsUser?> FindByPersonIDAsync(int? personID)
        {
            return clsUser.FindByPersonIDAsync(personID);
        }

        public Task<clsUser?> FindByIdAsync(int? userID)
        {
            return clsUser.FindUserAsync(userID);
        }

        public Task<PagedResult<clsUser.UserItem>> FindUsersAsync(string? fullname = null, string? email = null, bool? active = null, int pageNumber = 1, int pageSize = 10)
        {
            return clsUser.FindUsersAsync(fullname, email, active, pageNumber, pageSize);
        }

        public Task<bool> Deactivate(int? userId, int? actorUserId)
        {
            return clsUser.DeactivateUser(userId, actorUserId);
        }

        public Task<bool> Activate(int? userId, int? actorUserId)
        {
            return clsUser.ActivateUser(userId, actorUserId);
        }
  }
}