using Clinic_Business;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.People
{
    public class PersonOwnerOrAuthorizedHandler : AuthorizationHandler<PersonOwnerOrAuthorizedRequirement, int>
    {
        private readonly IUserRepository _userRepository;

        public PersonOwnerOrAuthorizedHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PersonOwnerOrAuthorizedRequirement requirement,
            int personId
        )
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            clsUser? userPerson = await _userRepository.FindByPersonIDAsync(personId);

            if (context.User.IsInRole("Admin") || context.User.IsInRole("Staff"))
                context.Succeed(requirement);

            if (
                int.TryParse(userId, out int authenticatedUserId) &&
                userPerson != null &&
                authenticatedUserId == userPerson.UserID
            )
            {
                context.Succeed(requirement);
            }
        }
    }
}
