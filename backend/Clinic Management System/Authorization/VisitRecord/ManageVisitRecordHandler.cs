using Clinic_Business;
using Clinic_Business.Interfaces;
using Clinic_Management_System.Authorization.Appointment;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.VisitRecord
{
    public class ManageVisitRecordHandler : AuthorizationHandler<ManageVisitRecordRequirement, int>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly ILogger<ManageVisitRecordHandler> _logger;

        public ManageVisitRecordHandler(
            IDoctorRepository doctorRepository,
            ILogger<ManageVisitRecordHandler> logger)
        {
            _doctorRepository = doctorRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ManageVisitRecordRequirement requirement, 
            int visitId
        )
        {
            if (context.User.IsInRole("Admin"))
                context.Succeed(requirement);

            if (context.User.IsInRole("Doctor"))
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(userId, out int authenticatedUserID) && await _doctorRepository.IsDoctorOfVisitAsync(visitId, authenticatedUserID))
                    context.Succeed(requirement);
            }

            if (!context.HasSucceeded)
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not manage the record of visit {VisitId}",
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier), visitId);
        }
    }
}
