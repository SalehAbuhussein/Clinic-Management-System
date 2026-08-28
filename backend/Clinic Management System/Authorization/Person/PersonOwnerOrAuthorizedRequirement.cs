using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.People
{
    public class PersonOwnerOrAuthorizedRequirement : IAuthorizationRequirement
    {
    }
}
