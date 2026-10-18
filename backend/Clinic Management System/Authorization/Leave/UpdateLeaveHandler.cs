using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Leave
{
    public class UpdateLeaveHandler : AuthorizationHandler<UpdateLeaveRequirement, clsLeave>
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<UpdateLeaveHandler> _logger;

        public UpdateLeaveHandler(IAuditRepository auditRepository,
            ILogger<UpdateLeaveHandler> logger)
        {
            _auditRepository = auditRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UpdateLeaveRequirement requirement,
            clsLeave leave
        )
        {
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only the requester edits their own reason, and only while an admin has yet to rule on it.
            if (leave.IsPending && int.TryParse(userId, out int authenticatedUserID) && leave.UserID == authenticatedUserID)
                context.Succeed(requirement);

            if (!context.HasSucceeded)
            {
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not update leave {LeaveId}",
                    userId, leave.LeaveID);

                await _auditRepository.CreateAsync(
                    int.TryParse(userId, out int actorId) ? actorId : null,
                    AuditAction.AccessDenied, "Leave", leave.LeaveID, $"update leave {leave.LeaveID}");
            }
        }
    }
}
