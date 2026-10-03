using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.VisitRecord
{
    public class ViewVisitRecordHandler : AuthorizationHandler<ViewVisitRecordRequirement, int>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly ILogger<ViewVisitRecordHandler> _logger;

        public ViewVisitRecordHandler(
            IDoctorRepository doctorRepository,
            ILogger<ViewVisitRecordHandler> logger)
        {
            _doctorRepository = doctorRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ViewVisitRecordRequirement requirement,
            int visitId
        )
        {
            if (context.User.IsInRole("Admin") || context.User.IsInRole("Staff"))
                context.Succeed(requirement);

            if (context.User.IsInRole("Doctor"))
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(userId, out int authenticatedUserID) && await _doctorRepository.IsDoctorOfVisitAsync(visitId, authenticatedUserID))
                    context.Succeed(requirement);
            }

            if (!context.HasSucceeded)
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not view the record of visit {VisitId}",
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier), visitId);
        }
    }
}
