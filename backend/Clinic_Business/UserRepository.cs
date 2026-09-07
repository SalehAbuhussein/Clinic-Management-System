using Clinic_Business.Interfaces;

namespace Clinic_Business
{
    public class UserRepository : IUserRepository
    {
        public clsUser? FindByEmail(string email)
        {
            return clsUser.FindByEmail(email);
        }
    }
}
