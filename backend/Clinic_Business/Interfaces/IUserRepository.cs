
namespace Clinic_Business.Interfaces
{
    public interface IUserRepository
    {
        Task<clsUser?> FindByEmailAsync(string email);
        Task<clsUser?> FindByPersonIDAsync(int? personID);
    }
}
