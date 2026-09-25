using Clinic_Business.Entities;
using Clinic_Business.Common;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Appointments")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IAppointmentRepository _appointmentRepository;

        public AppointmentsController(
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IAppointmentRepository appointmentRepository)
        {
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet(Name = "GetAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<AppointmentDTO>>> GetAppointments(
            int? doctorId,
            int? patientId,
            DateOnly? from,
            DateOnly? to,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 10)] int perPage = 10
        )
        {
            perPage = Math.Clamp(perPage, 1, 10);

            if (from.HasValue && to.HasValue && from > to)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "The start of the range must be on or before its end.");
            }

            PagedResult<clsAppointment> appointmentsPage = await _appointmentRepository.FindAllAsync(doctorId, patientId, from, to, page, perPage);

            List<AppointmentDTO> appointmentsList = appointmentsPage.Items
                .Select(appointment => new AppointmentDTO
                {
                    Id = appointment.AppointmentID,
                    DoctorId = appointment.DoctorID,
                    DoctorName = appointment.DoctorInfo?.UserInfo.PersonInfo.Fullname ?? string.Empty,
                    PatientId = appointment.PatientID,
                    PatientName = appointment.PatientInfo?.Fullname ?? string.Empty,
                    StartAt = appointment.StartAt,
                    EndAt = appointment.EndAt,
                    Status = appointment.Status,
                    Notes = appointment.Notes
                }).ToList();

            return Ok(new PagedResult<AppointmentDTO>(appointmentsList, appointmentsPage.Page, appointmentsPage.PerPage, appointmentsPage.Total));
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
            clsAppointment? appointment = await _appointmentRepository.FindByIdAsync(id);
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
                PatientId = appointment.PatientID,
                PatientName = appointment.PatientInfo?.Fullname ?? string.Empty,
                DoctorId = appointment.DoctorID,
                DoctorName = appointment.DoctorInfo?.UserInfo.PersonInfo.Fullname ?? string.Empty,
                StartAt = appointment.StartAt,
                EndAt = appointment.EndAt,
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
            if (!await _doctorRepository.ExistsAsync(appointmentDTO.DoctorID) ||
                !await _patientRepository.ExistsAsync(appointmentDTO.PatientID))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: "The doctor or patient referenced by this appointment does not exist.");

            if (await _appointmentRepository.HasPatientConflictAsync(appointmentDTO.PatientID, appointmentDTO.StartAt, appointmentDTO.EndAt))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "The patient already has an appointment in that time slot.");

            if (await _appointmentRepository.HasDoctorConflictAsync(appointmentDTO.DoctorID, appointmentDTO.StartAt, appointmentDTO.EndAt))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "The doctor already has an appointment in that time slot.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            clsAppointment newAppointment = new clsAppointment();
            newAppointment.DoctorID = appointmentDTO.DoctorID;
            newAppointment.PatientID = appointmentDTO.PatientID;
            newAppointment.StartAt = appointmentDTO.StartAt;
            newAppointment.EndAt = appointmentDTO.EndAt;
            newAppointment.Status = appointmentDTO.Status;
            newAppointment.Notes = appointmentDTO.Notes;

            if (int.TryParse(userId, out int parsedUserID))
                newAppointment.CreatedBy = parsedUserID;

            if (await newAppointment.SaveAsync())
            {
                AppointmentDTO newAppointmentDTO = new AppointmentDTO
                {
                    Id = newAppointment.AppointmentID,
                    PatientId = newAppointment.PatientID,
                    DoctorId = newAppointment.DoctorID,
                    StartAt = newAppointment.StartAt,
                    EndAt = newAppointment.EndAt,
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
        public async Task<ActionResult> UpdateAppointment([Range(1, int.MaxValue)] int id, UpdateAppointmentDTO updatedAppointmentDTO)
        {
            clsAppointment? appointment = await _appointmentRepository.FindByIdAsync(id);

            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            if (!await _doctorRepository.ExistsAsync(appointment.DoctorID))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"The doctor on appointment {id} no longer exists.");

            if (!appointment.IsValid())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment is not in a valid state to be updated.");

            DateTimeOffset newStart = updatedAppointmentDTO.StartAt;
            DateTimeOffset newEnd = updatedAppointmentDTO.EndAt;

            if (newEnd <= newStart)
                return Problem(statusCode: StatusCodes.Status400BadRequest,
                    detail: "EndAt must be after StartAt.");

            bool timeWindowChanged = newStart != appointment.StartAt || newEnd != appointment.EndAt;

            if (timeWindowChanged)
            {
                if (newStart < DateTime.UtcNow)
                    return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                        detail: "An appointment cannot be rescheduled into the past.");

                if (await _appointmentRepository.HasPatientConflictAsync(appointment.PatientID, newStart, newEnd, id))
                    return Problem(statusCode: StatusCodes.Status409Conflict,
                        detail: "The patient already has an appointment in that time slot.");

                if (await _appointmentRepository.HasDoctorConflictAsync(appointment.DoctorID, newStart, newEnd, id))
                    return Problem(statusCode: StatusCodes.Status409Conflict,
                        detail: "The doctor already has an appointment in that time slot.");
            }

            appointment.StartAt = newStart;
            appointment.EndAt = newEnd;
            appointment.Notes = updatedAppointmentDTO.Notes;

            if (await appointment.SaveAsync())
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
        public async Task<ActionResult<PagedResult<AppointmentDTO>>> GetDoctorAppointments(
            int doctorId,
            [FromServices] IAuthorizationService authorizationService,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 10)] int perPage = 10
        )
        {
            perPage = Math.Clamp(perPage, 1, 10);

            if (await _doctorRepository.FindByIdAsync(doctorId) == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Doctor with id {doctorId} not found");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                doctorId,
                "CanDoctorViewAppointment"
            );

            if (!authResult.Succeeded)
                return Forbid();

            PagedResult<clsAppointment> appointmentsPage = await _appointmentRepository.FindAllAsync(doctorId, null, null, null, page, perPage);

            List<AppointmentDTO> appointmentsList = appointmentsPage.Items
                .Select(appointment => new AppointmentDTO
                {
                    Id = appointment.AppointmentID,
                    DoctorId = appointment.DoctorID,
                    DoctorName = appointment.DoctorInfo?.UserInfo.PersonInfo.Fullname ?? string.Empty,
                    PatientId = appointment.PatientID,
                    PatientName = appointment.PatientInfo?.Fullname ?? string.Empty,
                    StartAt = appointment.StartAt,
                    EndAt = appointment.EndAt,
                    Status = appointment.Status,
                    Notes = appointment.Notes
                }).ToList();

            return Ok(new PagedResult<AppointmentDTO>(appointmentsList, appointmentsPage.Page, appointmentsPage.PerPage, appointmentsPage.Total));
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("doctor/{doctorId}/schedules", Name = "GetDoctorAppointmentSchedules")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<AppointmentScheduleDTO>>> GetDoctorAppointmentSchedules(
            [Range(1, int.MaxValue)] int doctorId,
            DateTime from,
            DateTime to
        )
        {
            List<clsAppointment.DoctorSchedule> doctorSchedules = await clsAppointment.FindDoctorSchedulesAsync(doctorId, from, to);

            List<AppointmentScheduleDTO> schedulesList = doctorSchedules
                .Select(schedule => new AppointmentScheduleDTO
                {
                    AppointmentID = schedule.AppointmentID,
                    DoctorID = schedule.DoctorID,
                    PatientID = schedule.PatientID,
                    StartAt = schedule.StartAt,
                    EndAt = schedule.EndAt,
                    Status = schedule.Status
                }).ToList();

            return Ok(schedulesList);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("{id}/attend", Name = "AttendAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> AttendAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = await _appointmentRepository.FindByIdAsync(id);
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

            if (await appointment.AttendAppointmentAsync())
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
        public async Task<ActionResult> UnattendAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = await _appointmentRepository.FindByIdAsync(id);
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

            if (await appointment.UnattendAppointmentAsync())
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
        public async Task<ActionResult> CancelAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = await _appointmentRepository.FindByIdAsync(id);
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

            if (await appointment.CancelAppointmentAsync())
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
        public async Task<ActionResult> DeleteAppointment([Range(1, int.MaxValue)] int id)
        {
            clsAppointment? appointment = await _appointmentRepository.FindByIdAsync(id);
            if (appointment == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {id} not found");

            if (await _appointmentRepository.DeleteAsync(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The appointment could not be deleted.");
        }

        // Stored appointment times are UTC, but datetime2 carries no Kind, so the value
        // arrives as Unspecified. SpecifyKind states what it is; without it, an implicit
    }
}
