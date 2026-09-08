using Clinic_Business.Interfaces;

namespace Clinic_Business
{
    public class UserRepository : IUserRepository
    {
        private static int _count;

        public UserRepository()
        {
            Console.WriteLine($"  [repo] constructed #{++_count}  {Guid.NewGuid().ToString()[..8]}");
        }

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