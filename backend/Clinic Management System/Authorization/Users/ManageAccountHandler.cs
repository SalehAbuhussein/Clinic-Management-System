using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Users
{
    /// <summary>
    /// Who may act on an account: change its password, correct its details.
    ///
    /// Anyone may act on their own. An admin may also act on a doctor's or a
    /// staff member's, because those are the accounts nobody else can fix. One
    /// admin may not touch another's: an admin who could do that could take
    /// over every account in the clinic.
    /// </summary>
    public class ManageAccountHandler : AuthorizationHandler<ManageAccountRequirement, clsUser>
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<ManageAccountHandler> _logger;

        public ManageAccountHandler(IAuditRepository auditRepository,
            ILogger<ManageAccountHandler> logger)
        {
            _auditRepository = auditRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ManageAccountRequirement requirement,
            clsUser target
        )
        {
            if (IsOwnAccount(context.User, target) || MayActOnAccountOf(context.User, target))
                context.Succeed(requirement);

            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!context.HasSucceeded)
            {
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not act on the account of user {TargetId}",
                    userId, target.UserID);

                await _auditRepository.CreateAsync(
                    int.TryParse(userId, out int actorId) ? actorId : null,
                    AuditAction.AccessDenied, "User", target.UserID, $"act on the account of user {target.UserID}");
            }
        }

        private static bool IsOwnAccount(ClaimsPrincipal caller, clsUser target)
            => int.TryParse(caller.FindFirstValue(ClaimTypes.NameIdentifier), out int id) && target.UserID == id;

        private static bool MayActOnAccountOf(ClaimsPrincipal caller, clsUser target)
            => caller.IsInRole("Admin") && target.Role != "Admin";
    }
}
