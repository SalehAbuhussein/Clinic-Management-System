using Clinic_Business.Entities;

namespace Clinic_Management_System.Services
{
    public interface ITokenService
    {
        TokenPair IssueFor(clsUser user);
    }
}
