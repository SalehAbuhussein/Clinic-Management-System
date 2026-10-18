using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Appointment
{
    public class CanDoctorViewAppointmentHandler : AuthorizationHandler<CanDoctorViewAppointmentRequirement, int>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<CanDoctorViewAppointmentHandler> _logger;

        public CanDoctorViewAppointmentHandler(
            IDoctorRepository doctorRepository,
            IAuditRepository auditRepository,
            ILogger<CanDoctorViewAppointmentHandler> logger)
        {
            _doctorRepository = doctorRepository;
            _auditRepository = auditRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            CanDoctorViewAppointmentRequirement requirement,
            int doctorID
        )
        {
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Doctor => Can View if the appointment related to him
            // Admin and Staff => Always can access patient 
            if (context.User.IsInRole("Admin") || context.User.IsInRole("Staff"))
                context.Succeed(requirement);

            if (context.User.IsInRole("Doctor"))
            {
                if (int.TryParse(userId, out int authenticatedUserId))
                {
                    clsDoctor? doctor = await _doctorRepository.FindByUserIdAsync(authenticatedUserId);

                    if (doctor != null)
                    {
                        if (doctor.DoctorID == doctorID)
                            context.Succeed(requirement);
                    }
                }
            }

            if (!context.HasSucceeded)
            {
                _logger.LogWarning(
                    "Authorization denied: user {UserId} may not view the appointments of doctor {DoctorId}",
                    userId, doctorID);

                await _auditRepository.CreateAsync(
                    int.TryParse(userId, out int actorId) ? actorId : null,
                    AuditAction.AccessDenied, "Doctor", doctorID, $"view the appointments of doctor {doctorID}");
            }
        }
    }
}
