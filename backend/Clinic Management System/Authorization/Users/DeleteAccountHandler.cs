using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Users
{
    public class DeleteAccountHandler : AuthorizationHandler<DeleteAccountRequirement, clsUser>
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<DeleteAccountHandler> _logger;

        public DeleteAccountHandler(IAuditRepository auditRepository, ILogger<DeleteAccountHandler> logger)
        {
            _auditRepository = auditRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DeleteAccountRequirement requirement,
            clsUser target
        )
        {
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isOwnAccount = int.TryParse(userId, out int callerId) && target.UserID == callerId;

            if (context.User.IsInRole("Admin") && target.Role != "Admin" && !isOwnAccount)
                context.Succeed(requirement);

            if (!context.HasSucceeded)
            {
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not delete the account of user {TargetId}",
                    userId, target.UserID);

                await _auditRepository.CreateAsync(
                    int.TryParse(userId, out int actorId) ? actorId : null,
                    AuditAction.AccessDenied, "User", target.UserID, $"delete the account of user {target.UserID}");
            }
        }
    }
}
