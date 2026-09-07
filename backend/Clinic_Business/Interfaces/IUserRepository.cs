
namespace Clinic_Business.Interfaces
{
    public interface IUserRepository
    {
        clsUser? FindByEmail(string email);
    }
}
