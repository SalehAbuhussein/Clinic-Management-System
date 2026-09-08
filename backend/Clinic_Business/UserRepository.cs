using Clinic_Business.Interfaces;

namespace Clinic_Business
{
    public class UserRepository : IUserRepository
    {
        public Task<clsUser?> FindByEmailAsync(string email)
        {
            return clsUser.FindByEmailAsync(email);
        }

        Task<clsUser?> IUserRepository.FindByPersonIDAsync(int? personID)
        {
            return clsUser.FindByPersonIDAsync(personID);
        }
    }
}