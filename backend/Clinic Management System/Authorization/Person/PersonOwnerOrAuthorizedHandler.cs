using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.People
{
    public class PersonOwnerOrAuthorizedHandler : AuthorizationHandler<PersonOwnerOrAuthorizedRequirement, int>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<PersonOwnerOrAuthorizedHandler> _logger;

        public PersonOwnerOrAuthorizedHandler(
            IUserRepository userRepository,
            ILogger<PersonOwnerOrAuthorizedHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
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

            // These handlers deny by never calling Succeed, so there is no failure
            // branch to log from — the absence of success at the end is the denial.
            if (!context.HasSucceeded)
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not access person {PersonId}",
                    userId, personId);
        }
    }
}
