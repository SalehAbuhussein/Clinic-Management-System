using Clinic_Business;
using Clinic_Management_System.Authorization.Appointment;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic_Management_System.Authorization.Prescription
{
    public class ManagePrescriptionHandler : AuthorizationHandler<ManagePrescriptionRequirement, int>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ManagePrescriptionRequirement requirement, 
            int visitId
        )
        {
            if (context.User.IsInRole("Admin"))
                context.Succeed(requirement);

            if (context.User.IsInRole("Doctor"))
            {
                clsVisit? visit = clsVisit.Find(visitId);

                if (visit != null)
                {
                    clsAppointment? appointment = visit.AppointmentInfo;
                    clsDoctor? doctor = appointment?.DoctorInfo;
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (doctor?.UserID.ToString() == userId)
                        context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
