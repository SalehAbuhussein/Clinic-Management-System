using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Appointments")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet(Name = "GetAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<List<AppointmentDTO>> GetAppointments(
            int? doctorId,
            int? patientId,
            DateTime? date,
            int page = 1, 
            int perPage = 10
        )
        {
            if (page < 1 || perPage < 1)
                return BadRequest("Invalid parameters");

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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentDTO>> GetAppointmentByID(
            int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            if (id < 1)
                return BadRequest("Invalid appointment id");

            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return NotFound($"Appointment with id {id} not found");

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
        public ActionResult<AppointmentDTO> AddAppointment(CreateAppointmentDTO appointmentDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid appointment data");

            if (!clsDoctor.IsDoctorExist(appointmentDTO.DoctorID) || !clsPatient.IsPatientExist(appointmentDTO.PatientID))
                return BadRequest("Invalid ids");

            if (clsAppointment.HasPatientExistingAppointment(appointmentDTO.PatientID, appointmentDTO.StartAt.UtcDateTime, appointmentDTO.EndAt.UtcDateTime))
                return BadRequest("Patient has existing appointment in the given time");

            if (clsAppointment.HasDoctorExistingAppointment(appointmentDTO.DoctorID, appointmentDTO.StartAt.UtcDateTime, appointmentDTO.EndAt.UtcDateTime))
                return BadRequest("Doctor has existing appointment in the given time");

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

            return BadRequest("Invalid appointment data");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id}", Name = "UpdateAppointment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult UpdateAppointment(int id, UpdateAppointmentDTO updatedAppointmentDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid appointment data");

            if (id < 1)
                return BadRequest("Invalid appointment id");

            clsAppointment? appointment = clsAppointment.Find(id);

            if (appointment == null)
                return NotFound($"Appointment with id {id} not found");

            if (!clsDoctor.IsDoctorExist(appointment.DoctorID))
                return NotFound($"Appointment with id {id} not found");

            if (!appointment.IsValid())
                return BadRequest("Invalid appointment data");

            DateTime newStart = updatedAppointmentDTO.StartAt.UtcDateTime;
            DateTime newEnd = updatedAppointmentDTO.EndAt.UtcDateTime;

            if (newEnd <= newStart)
                return BadRequest("EndAt must be after StartAt");

            bool timeWindowChanged = newStart != appointment.StartAt || newEnd != appointment.EndAt;

            if (timeWindowChanged)
            {
                if (newStart < DateTime.UtcNow)
                    return BadRequest("An appointment cannot be rescheduled into the past");

                if (clsAppointment.HasPatientExistingAppointment(appointment.PatientID, newStart, newEnd, id))
                    return BadRequest("Patient has exsting appointment in the given time");

                if (clsAppointment.HasDoctorExistingAppointment(appointment.DoctorID, newStart, newEnd, id))
                    return BadRequest("Doctor has existing appointment in the given time");
            }

            appointment.StartAt = newStart;
            appointment.EndAt = newEnd;
            appointment.Notes = updatedAppointmentDTO.Notes;

            if (appointment.Save())
                return Ok($"Appointment with id {id} has been updated successfully");

            return BadRequest("Invalid appointment data");
        }

        [Authorize]
        [HttpGet("doctor/{doctorId}", Name = "GetDoctorAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<AppointmentDTO>>> GetDoctorAppointments(
            int doctorId,
            [FromServices] IAuthorizationService authorizationService,
            int page = 1,
            int perPage = 10
        )
        {
            if (page < 1 || perPage < 1)
                return BadRequest("Invalid parameters");

            perPage = Math.Clamp(perPage, 1, 10);

            if (clsDoctor.Find(doctorId) == null)
                return NotFound($"Doctor with id {doctorId} not found");

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
        [HttpPatch("attend", Name = "AttendAppointment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult AttendAppointment(int id)
        {
            if (id < 1)
                return BadRequest("Invalid appointment id");

            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return NotFound($"Appointment with id {id} not found");

            if (appointment.IsCancelled())
                return BadRequest("Appointment has already been cancelled");

            if (appointment.IsAttended())
                return BadRequest("Appointment has already been attended before");

            if (appointment.IsNotAttended())
                return BadRequest("Appointment date has run by");

            if (appointment.AttendAppointment())
                return Ok($"Appointment with id {id} has been marked at attended");

            return BadRequest("Invalid Appointment data");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("no-show", Name = "UnattendAppointment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UnattendAppointment(int id)
        {
            if (id < 1)
                return BadRequest("Invalid appointment id");

            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return NotFound($"Appointment with id {id} not found");

            if (appointment.IsCancelled())
                return BadRequest("Appointment has already been cancelled");

            if (appointment.IsNotAttended())
                return BadRequest("Appointment is already not attended");

            if (appointment.IsAttended())
                return BadRequest("Appointment is already attended");

            if (appointment.UnattendAppointment())
                return Ok($"Appointment with id {id} has been marked at unattended");

            return BadRequest("Invalid Appointment data");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("cancel", Name = "CancelAppointment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult CancelAppointment(int id)
        {
            if (id < 1)
                return BadRequest("Invalid appointment id");

            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return NotFound($"Appointment with id {id} not found");

            if (appointment.IsCancelled())
                return BadRequest("Appointment has already been cancelled");

            if (appointment.IsNotAttended())
                return BadRequest("Appointment is already not attended");

            if (appointment.IsAttended())
                return BadRequest("Appointment is already attended");

            if (appointment.CancelAppointment())
                return Ok($"Appointment with id {id} has been cancelled");

            return BadRequest("Invalid Appointment data");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeleteAppointment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult DeleteAppointment(int id)
        {
            if (id < 1)
                return BadRequest("Invalid medicine id");

            clsAppointment? appointment = clsAppointment.Find(id);
            if (appointment == null)
                return NotFound($"Appointment with id {id} not found");

            if (clsAppointment.Delete(id))
                return Ok($"Medicine with id of {id} has been deleted.");

            return BadRequest("Invalid medicine id");
        }

        // Stored appointment times are UTC, but datetime2 carries no Kind, so the value
        // arrives as Unspecified. SpecifyKind states what it is; without it, an implicit
        // DateTime -> DateTimeOffset conversion would stamp the server's local offset,
        // and a Kind=Local value would throw instead.
        private static DateTimeOffset AsUtc(DateTime stored) => new DateTimeOffset(DateTime.SpecifyKind(stored, DateTimeKind.Utc), TimeSpan.Zero);
    }
}
