using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.VisitRecord
{
    public class ViewVisitRecordHandler : AuthorizationHandler<ViewVisitRecordRequirement, int>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<ViewVisitRecordHandler> _logger;

        public ViewVisitRecordHandler(
            IDoctorRepository doctorRepository,
            IAuditRepository auditRepository,
            ILogger<ViewVisitRecordHandler> logger)
        {
            _doctorRepository = doctorRepository;
            _auditRepository = auditRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ViewVisitRecordRequirement requirement,
            int visitId
        )
        {
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (context.User.IsInRole("Admin") || context.User.IsInRole("Staff"))
                context.Succeed(requirement);

            if (context.User.IsInRole("Doctor"))
            {
                if (int.TryParse(userId, out int authenticatedUserID) && await _doctorRepository.IsDoctorOfVisitAsync(visitId, authenticatedUserID))
                    context.Succeed(requirement);
            }

            if (!context.HasSucceeded)
            {
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not view the record of visit {VisitId}",
                    userId, visitId);

                await _auditRepository.CreateAsync(
                    int.TryParse(userId, out int actorId) ? actorId : null,
                    AuditAction.AccessDenied, "Visit", visitId, $"view the record of visit {visitId}");
            }
        }
    }
}
