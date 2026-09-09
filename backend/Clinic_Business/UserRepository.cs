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
    }
}