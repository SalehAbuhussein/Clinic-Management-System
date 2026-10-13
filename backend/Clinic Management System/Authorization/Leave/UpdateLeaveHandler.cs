using Clinic_Business.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Leave
{
    public class UpdateLeaveHandler : AuthorizationHandler<UpdateLeaveRequirement, clsLeave>
    {
        private readonly ILogger<UpdateLeaveHandler> _logger;

        public UpdateLeaveHandler(ILogger<UpdateLeaveHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
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
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not update leave {LeaveId}",
                    userId, leave.LeaveID);

            return Task.CompletedTask;
        }
    }
}
