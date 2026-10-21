using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Patient
{
    /// <summary>
    /// Admin and staff run the front desk and see every patient. A doctor sees
    /// only the patients they have an appointment with, the same rule
    /// ViewVisitRecordHandler applies to a visit. Without it a doctor barred
    /// from a visit could still read the patient behind it by id.
    /// </summary>
    public class ViewPatientHandler : AuthorizationHandler<ViewPatientRequirement, int>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<ViewPatientHandler> _logger;

        public ViewPatientHandler(
            IDoctorRepository doctorRepository,
            IAuditRepository auditRepository,
            ILogger<ViewPatientHandler> logger)
        {
            _doctorRepository = doctorRepository;
            _auditRepository = auditRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ViewPatientRequirement requirement,
            int patientId
        )
        {
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (context.User.IsInRole("Admin") || context.User.IsInRole("Staff"))
                context.Succeed(requirement);

            if (context.User.IsInRole("Doctor"))
            {
                if (int.TryParse(userId, out int authenticatedUserID) && await _doctorRepository.IsDoctorOfPatientAsync(patientId, authenticatedUserID))
                    context.Succeed(requirement);
            }

            if (!context.HasSucceeded)
            {
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not view the record of patient {PatientId}",
                    userId, patientId);

                await _auditRepository.CreateAsync(
                    int.TryParse(userId, out int actorId) ? actorId : null,
                    AuditAction.AccessDenied, "Patient", patientId, $"view the record of patient {patientId}");
            }
        }
    }
}
