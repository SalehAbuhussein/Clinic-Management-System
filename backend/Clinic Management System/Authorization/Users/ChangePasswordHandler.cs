using Clinic_Business.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Users
{
    /// <summary>
    /// Who may set an account's password.
    ///
    /// Anyone may set their own. An admin may also reset a doctor's or a
    /// staff member's, because those are the accounts nobody else can get
    /// back into. One admin may not reset another's: an admin who could do
    /// that could take over every account in the clinic.
    /// </summary>
    public class ChangePasswordHandler : AuthorizationHandler<ChangePasswordRequirement, clsUser>
    {
        private readonly ILogger<ChangePasswordHandler> _logger;

        public ChangePasswordHandler(ILogger<ChangePasswordHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ChangePasswordRequirement requirement,
            clsUser target
        )
        {
            if (IsOwnAccount(context.User, target) || MayResetAccountOf(context.User, target))
                context.Succeed(requirement);

            if (!context.HasSucceeded)
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not set the password of user {TargetId}",
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier), target.UserID);

            return Task.CompletedTask;
        }

        private static bool IsOwnAccount(ClaimsPrincipal caller, clsUser target)
            => int.TryParse(caller.FindFirstValue(ClaimTypes.NameIdentifier), out int id) && target.UserID == id;

        private static bool MayResetAccountOf(ClaimsPrincipal caller, clsUser target)
            => caller.IsInRole("Admin") && target.Role != "Admin";
    }
}
