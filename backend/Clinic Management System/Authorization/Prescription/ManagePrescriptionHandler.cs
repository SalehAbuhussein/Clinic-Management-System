using Clinic_Business;
using Clinic_Business.Interfaces;
using Clinic_Management_System.Authorization.Appointment;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Prescription
{
    public class ManagePrescriptionHandler : AuthorizationHandler<ManagePrescriptionRequirement, int>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly ILogger<ManagePrescriptionHandler> _logger;

        public ManagePrescriptionHandler(
            IDoctorRepository doctorRepository,
            ILogger<ManagePrescriptionHandler> logger)
        {
            _doctorRepository = doctorRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ManagePrescriptionRequirement requirement, 
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
                    "Authorization denied: user {UserId} may not manage the prescription of visit {VisitId}",
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier), visitId);
        }
    }
}
