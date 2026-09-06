using Clinic_Business;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Appointment
{
    public class CanDoctorViewAppointmentHandler : AuthorizationHandler<CanDoctorViewAppointmentRequirement, int>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            CanDoctorViewAppointmentRequirement requirement,
            int doctorID
        )
        {
            // Doctor => Can View if the appointment related to him

            // Admin and Staff => Always can access patient 
            if (context.User.IsInRole("Admin") || context.User.IsInRole("Staff"))
                context.Succeed(requirement);

            if (context.User.IsInRole("Doctor"))
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(userId, out int authenticatedUserId))
                {
                    clsDoctor? doctor = clsDoctor.FindByUserID(authenticatedUserId);

                    if (doctor != null)
                    {
                        if (doctor.DoctorID == doctorID)
                            context.Succeed(requirement);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
