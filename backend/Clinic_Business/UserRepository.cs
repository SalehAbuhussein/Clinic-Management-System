using Clinic_Business.Interfaces;

namespace Clinic_Business
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

        public Task<List<clsUser.UserItem>> FindUsersAsync(int pageNumber = 1, int pageSize = 10)
        {
            return clsUser.FindUsersAsync(pageNumber, pageSize);
        }

        public Task<bool> Deactivate(int? userId)
        {
            return clsUser.DeactivateUser(userId);
        }

        public Task<bool> Activate(int? userId)
        {
            return clsUser.ActivateUser(userId);
        }
  }
}