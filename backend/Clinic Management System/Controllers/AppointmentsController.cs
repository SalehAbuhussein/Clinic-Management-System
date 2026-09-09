using Clinic_Business;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Appointments")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IPatientRepository _patientRepository;

        public AppointmentsController(IPatientRepository patientRepository)
        {
            _patientRepository = patientRepository;
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet(Name = "GetAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<List<AppointmentDTO>> GetAppointments(
            int? doctorId,
            int? patientId,
            DateTime? date,
            [Range(1, int.MaxValue)] int page = 1, 
            [Range(1, 10)] int perPage = 10
        )
        {
            perPage = Math.Clamp(perPage, 1, 10);

            DataTable table = clsAppointment.FindAll(doctorId, patientId, date, page, perPage);
            List<AppointmentDTO> appointmentsList = new List<AppointmentDTO>();

            foreach (DataRow row in table.Rows)
            {
                appointmentsList.Add(new AppointmentDTO
                {
                    Id = row.Field<int>("AppointmentID"),
                    DoctorID = row.Field<int>("DoctorID"),
                    PatientID = row.Field<int>("PatientID"),
                    StartAt = AsUtc(row.Field<DateTime>("StartAt")),
                    EndAt = AsUtc(row.Field<DateTime>("EndAt")),
                    Status = row.Field<byte>("Status"),
                    Notes = row.Field<string>("Notes")
                });
            }

            return Ok(appointmentsList);
        }

        [Authorize(Roles = "Admin,Doctor,Staff")]
        [HttpGet("{id}", Name = "GetAppointmentByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentDTO>> GetAppointmentByID(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                appointment.DoctorID,
                "CanDoctorViewAppointment"
            );

            if (!authResult.Succeeded)
                return Forbid();

            AppointmentDTO appointmentDTO = new AppointmentDTO
            {
                Id = id,
                PatientID = appointment.PatientID,
                DoctorID = appointment.DoctorID,
                StartAt = AsUtc(appointment.StartAt),
                EndAt = AsUtc(appointment.EndAt),
                Notes = appointment.Notes,
                Status = appointment.Status,
            };

            return Ok(appointmentDTO);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost(Name = "AddAppointment")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<AppointmentDTO>> AddAppointment(CreateAppointmentDTO appointmentDTO)
        {
            if (!clsDoctor.IsDoctorExist(appointmentDTO.DoctorID) ||
                !await _patientRepository.ExistsAsync(appointmentDTO.PatientID))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: "The doctor or patient referenced by this appointment does not exist.");

            if (clsAppointment.HasPatientExistingAppointment(appointmentDTO.PatientID, appointmentDTO.StartAt.UtcDateTime, appointmentDTO.EndAt.UtcDateTime))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "The patient already has an appointment in that time slot.");

            if (clsAppointment.HasDoctorExistingAppointment(appointmentDTO.DoctorID, appointmentDTO.StartAt.UtcDateTime, appointmentDTO.EndAt.UtcDateTime))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "The doctor already has an appointment in that time slot.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            clsAppointment newAppointment = new clsAppointment();
            newAppointment.DoctorID = appointmentDTO.DoctorID;
            newAppointment.PatientID = appointmentDTO.PatientID;
            newAppointment.StartAt = appointmentDTO.StartAt.UtcDateTime;
            newAppointment.EndAt = appointmentDTO.EndAt.UtcDateTime;
            newAppointment.Status = appointmentDTO.Status;
            newAppointment.Notes = appointmentDTO.Notes;

            if (int.TryParse(userId, out int parsedUserID))
                newAppointment.CreatedBy = parsedUserID;

            if (newAppointment.Save())
            {
                AppointmentDTO newAppointmentDTO = new AppointmentDTO
                {
                    Id = newAppointment.AppointmentID,
                    PatientID = newAppointment.PatientID,
                    DoctorID = newAppointment.DoctorID,
                    StartAt = AsUtc(newAppointment.StartAt),
                    EndAt = AsUtc(newAppointment.EndAt),
                    Notes = newAppointment.Notes,
                    Status = newAppointment.Status,
                };
                return CreatedAtRoute("GetAppointmentByID", new { id = newAppointment.AppointmentID }, newAppointmentDTO);
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The appointment could not be saved. Check the doctor, patient and time range.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id}", Name = "UpdateAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult UpdateAppointment([Range(1, int.MaxValue)] int id, UpdateAppointmentDTO updatedAppointmentDTO)
        {
            clsAppointment? appointment = clsAppointment.Find(id);

            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            if (!clsDoctor.IsDoctorExist(appointment.DoctorID))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"The doctor on appointment {id} no longer exists.");

            if (!appointment.IsValid())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment is not in a valid state to be updated.");

            DateTime newStart = updatedAppointmentDTO.StartAt.UtcDateTime;
            DateTime newEnd = updatedAppointmentDTO.EndAt.UtcDateTime;

            if (newEnd <= newStart)
                return Problem(statusCode: StatusCodes.Status400BadRequest,
                    detail: "EndAt must be after StartAt.");

            bool timeWindowChanged = newStart != appointment.StartAt || newEnd != appointment.EndAt;

            if (timeWindowChanged)
            {
                if (newStart < DateTime.UtcNow)
                    return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                        detail: "An appointment cannot be rescheduled into the past.");

                if (clsAppointment.HasPatientExistingAppointment(appointment.PatientID, newStart, newEnd, id))
                    return Problem(statusCode: StatusCodes.Status409Conflict,
                        detail: "The patient already has an appointment in that time slot.");

                if (clsAppointment.HasDoctorExistingAppointment(appointment.DoctorID, newStart, newEnd, id))
                    return Problem(statusCode: StatusCodes.Status409Conflict,
                        detail: "The doctor already has an appointment in that time slot.");
            }

            appointment.StartAt = newStart;
            appointment.EndAt = newEnd;
            appointment.Notes = updatedAppointmentDTO.Notes;

            if (appointment.Save())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The appointment could not be saved. Check the doctor, patient and time range.");
        }

        [Authorize]
        [HttpGet("doctor/{doctorId}", Name = "GetDoctorAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<AppointmentDTO>>> GetDoctorAppointments(
            int doctorId,
            [FromServices] IAuthorizationService authorizationService,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 10)] int perPage = 10
        )
        {
            perPage = Math.Clamp(perPage, 1, 10);

            if (clsDoctor.Find(doctorId) == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Doctor with id {doctorId} not found");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                doctorId,
                "CanDoctorViewAppointment"
            );

            if (!authResult.Succeeded)
                return Forbid();

            DataTable table = clsAppointment.FindAll(doctorId, null, null, page, perPage);
            List<AppointmentDTO> appointmentsList = new List<AppointmentDTO>();

            foreach (DataRow row in table.Rows)
            {
                appointmentsList.Add(new AppointmentDTO
                {
                    Id = row.Field<int>("AppointmentID"),
                    DoctorID = row.Field<int>("DoctorID"),
                    PatientID = row.Field<int>("PatientID"),
                    StartAt = AsUtc(row.Field<DateTime>("StartAt")),
                    EndAt = AsUtc(row.Field<DateTime>("EndAt")),
                    Status = row.Field<byte>("Status"),
                    Notes = row.Field<string>("Notes")
                });
            }

            return Ok(appointmentsList);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("{id}/attend", Name = "AttendAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult AttendAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            if (appointment.IsCancelled())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment has already been cancelled.");

            if (appointment.IsAttended())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment has already been marked attended.");

            if (appointment.IsNotAttended())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment date has passed.");

            if (appointment.AttendAppointment())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The appointment could not be marked attended.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("{id}/no-show", Name = "UnattendAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult UnattendAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            if (appointment.IsCancelled())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment has already been cancelled.");

            if (appointment.IsNotAttended())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment is already marked as not attended.");

            if (appointment.IsAttended())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment has already been marked attended.");

            if (appointment.UnattendAppointment())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The appointment could not be marked as not attended.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("{id}/cancel", Name = "CancelAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult CancelAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            if (appointment.IsCancelled())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment has already been cancelled.");

            if (appointment.IsNotAttended())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment is already marked as not attended.");

            if (appointment.IsAttended())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment has already been marked attended.");

            if (appointment.CancelAppointment())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The appointment could not be cancelled.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeleteAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult DeleteAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            if (clsAppointment.Delete(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The appointment could not be deleted.");
        }

        // Stored appointment times are UTC, but datetime2 carries no Kind, so the value
        // arrives as Unspecified. SpecifyKind states what it is; without it, an implicit
        // DateTime -> DateTimeOffset conversion would stamp the server's local offset,
        // and a Kind=Local value would throw instead.
        private static DateTimeOffset AsUtc(DateTime stored) => new DateTimeOffset(DateTime.SpecifyKind(stored, DateTimeKind.Utc), TimeSpan.Zero);
    }
}
