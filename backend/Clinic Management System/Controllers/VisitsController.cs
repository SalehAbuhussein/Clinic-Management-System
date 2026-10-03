using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Visits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Visits")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IVisitRepository _visitRepository;
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly IDoctorRepository _doctorRepository;

        public VisitsController(
            IAppointmentRepository appointmentRepository,
            IVisitRepository visitRepository,
            IDiagnosisRepository diagnosisRepository,
            IDoctorRepository doctorRepository)
        {
            _appointmentRepository = appointmentRepository;
            _visitRepository = visitRepository;
            _diagnosisRepository = diagnosisRepository;
            _doctorRepository = doctorRepository;
        }

        [Authorize(Roles = "Admin,Staff,Doctor")]
        [HttpGet(Name = "GetVisits")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<VisitDTO>>> GetVisits(int? doctorId, DateOnly? day, [Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            perPage = Math.Clamp(perPage, 1, 10);

            // If Docotor tries to get visits get them the visits of there own only 
            if (User.IsInRole("Doctor"))
            {
                clsDoctor? doctor = await _doctorRepository.FindByUserIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

                if (doctor == null)
                    return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "This account has the Doctor role but no doctor record.");

                doctorId = doctor.DoctorID;
            }

            PagedResult<clsVisit.VisitItem> visitsPage = await _visitRepository.FindAllAsync(doctorId, day, page, perPage);

            List<VisitDTO> visitsList = visitsPage.Items
                .Select(visit => new VisitDTO
                {
                    Id = visit.Id,
                    AppointmentID = visit.AppointmentID,
                    PatientName = visit.PatientName,
                    DoctorName = visit.DoctorName,
                    AppointmentStartAt = visit.AppointmentStartAt,
                    CheckinTime = visit.CheckinTime,
                    CheckoutTime = visit.CheckoutTime,
                    Notes = visit.Notes,
                }).ToList();

            return Ok(new PagedResult<VisitDTO>(visitsList, visitsPage.Page, visitsPage.PerPage, visitsPage.Total));
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpGet("{id}", Name = "GetVisitByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VisitDTO>> GetVisitByID([Range(1, int.MaxValue)] int id)
        {
            clsVisit? visit = await _visitRepository.FindByIdAsync(id);
            if (visit == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit with id {id} not found");

            VisitDTO visitDTO = new VisitDTO
            {
                Id = visit.VisitID,
                AppointmentID = visit.AppointmentID,
                CheckinTime = visit.CheckinTime,
                CheckoutTime = visit.CheckoutTime,
                Notes = visit.Notes,
            };

            return Ok(visitDTO);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost(Name = "AddVisit")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<VisitDTO>> AddVisit(CreateVisitDTO visitDTO)
        {
            if (await _visitRepository.ExistsByAppointmentIdAsync(visitDTO.AppointmentID))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: $"Appointment {visitDTO.AppointmentID} already has a visit.");

            if (!await _appointmentRepository.ExistsAsync(visitDTO.AppointmentID))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Appointment with id {visitDTO.AppointmentID} not found");

            if (await _appointmentRepository.IsCancelledAsync(visitDTO.AppointmentID))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The appointment has been cancelled, so no visit can be created for it.");

            DateTimeOffset checkinTime = visitDTO.CheckinTime ?? DateTimeOffset.Now;

            if (checkinTime > DateTimeOffset.UtcNow)
                return Problem(statusCode: StatusCodes.Status400BadRequest,
                    detail: "Checkin time cannot be in the future.");

            clsVisit newVisit = new clsVisit();
            newVisit.AppointmentID = visitDTO.AppointmentID;
            newVisit.CheckinTime = checkinTime;
            newVisit.Notes = visitDTO.Notes;

            if (await newVisit.SaveAsync())
                return CreatedAtRoute("GetVisitByID", new { id = newVisit.VisitID }, new VisitDTO
                {
                    Id = newVisit.VisitID,
                    AppointmentID = newVisit.AppointmentID,
                    CheckinTime = newVisit.CheckinTime,
                    CheckoutTime = newVisit.CheckoutTime,
                    Notes = newVisit.Notes,
                });

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The visit could not be saved.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id}", Name = "updatedVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateVisit([Range(1, int.MaxValue)] int id, UpdateVisitDTO visitDTO)
        {
            clsVisit? visit = await _visitRepository.FindByIdAsync(id);
            if (visit == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit with id {id} not found");

            if (visit.IsLocked)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit has already been finalized.");

            if (visitDTO.CheckinTime is not DateTimeOffset checkinTime)
                return Problem(statusCode: StatusCodes.Status400BadRequest,
                    detail: "Checkin time is required.");

            DateTimeOffset checkinTimeUtc = checkinTime;

            if (visit.CheckoutTime.HasValue && checkinTimeUtc > visit.CheckoutTime.Value)
                return Problem(statusCode: StatusCodes.Status400BadRequest,
                    detail: "Checkin time cannot be later than the checkout time.");
            else
            {
                if (checkinTimeUtc > DateTimeOffset.UtcNow)
                    return Problem(statusCode: StatusCodes.Status400BadRequest,
                        detail: "Checkin time cannot be in the future.");
            }

            // Allow Admin to modify the resource
            if (visit.IsLocked && !User.IsInRole("Admin"))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit is locked and can only be changed by an admin.");

            visit.CheckinTime = checkinTimeUtc;
            visit.Notes = visitDTO.Notes;

            if (await visit.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The visit could not be saved.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("{id}/checkout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> CheckoutVisit([Range(1, int.MaxValue)] int id)
        {
            clsVisit? visit = await _visitRepository.FindByIdAsync(id);
            if (visit == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit with id {id} not found");

            clsDiagnosis? diagnosis = await _diagnosisRepository.FindByVisitIdAsync(id);
            if (diagnosis == null)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "A visit must have a diagnosis before it can be checked out.");

            if (visit.IsLocked)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit has already been finalized.");

            visit.CheckoutTime = DateTime.UtcNow;
            visit.IsLocked = true;

            if (await visit.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The visit could not be checked out.");
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpDelete("{id}", Name = "DeleteVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeleteVisit([Range(1, int.MaxValue)] int id)
        {
            clsVisit? visit = await _visitRepository.FindByIdAsync(id);
            if (visit == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit with id {id} not found");

            if (visit.IsLocked)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit has already been finalized.");

            if (User.IsInRole("Staff"))
            {
                if (visit.IsLocked)
                    return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                        detail: "The visit has already been finalized.");
            }

            if (await _visitRepository.DeleteAsync(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The visit could not be deleted.");
        }
    }
}
