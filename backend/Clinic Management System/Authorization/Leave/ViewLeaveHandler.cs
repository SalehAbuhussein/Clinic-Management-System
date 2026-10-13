using Clinic_Business.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Leave
{
    public class ViewLeaveHandler : AuthorizationHandler<ViewLeaveRequirement, clsLeave>
    {
        private readonly ILogger<ViewLeaveHandler> _logger;

        public ViewLeaveHandler(ILogger<ViewLeaveHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ViewLeaveRequirement requirement,
            clsLeave leave
        )
        {
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // An admin reviews every request, so they read any leave.
            if (context.User.IsInRole("Admin"))
                context.Succeed(requirement);

            if (int.TryParse(userId, out int authenticatedUserID) && leave.UserID == authenticatedUserID)
                context.Succeed(requirement);

            if (!context.HasSucceeded)
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not view leave {LeaveId}",
                    userId, leave.LeaveID);

            return Task.CompletedTask;
        }
    }
}
