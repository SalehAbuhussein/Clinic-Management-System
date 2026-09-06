using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Visits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Visits")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet(Name = "GetVisits")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<VisitDTO> GetVisits([Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            perPage = Math.Clamp(perPage, 1, 10);

            DataTable table = clsVisit.FindAll(page, perPage);
            List<VisitDTO> visitsList = new List<VisitDTO>();

            foreach (DataRow row in table.Rows)
            {
                visitsList.Add(new VisitDTO
                {
                    Id = row.Field<int>("VisitID"),
                    AppointmentID = row.Field<int>("AppointmentID"),
                    CheckinTime = AsUtc(row.Field<DateTime>("CheckinTime")),
                    CheckoutTime = AsUtc(row.Field<DateTime?>("CheckoutTime")),
                    Notes = row.Field<string>("Notes"),
                });
            }

            return Ok(visitsList);
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpGet("{id}", Name = "GetVisitByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<VisitDTO> GetVisitByID([Range(1, int.MaxValue)] int id)
        {
            clsVisit? visit = clsVisit.Find(id);
            if (visit == null)
                return NotFound($"Visit with id {id} not found");

            VisitDTO visitDTO = new VisitDTO
            {
                Id = visit.VisitID,
                AppointmentID = visit.AppointmentID,
                CheckinTime = AsUtc(visit.CheckinTime),
                CheckoutTime = AsUtc(visit.CheckoutTime),
                Notes = visit.Notes,
            };

            return Ok(visitDTO);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost(Name = "AddVisit")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<VisitDTO> AddVisit(CreateVisitDTO visitDTO)
        {
            if (clsVisit.IsExistByAppointmentID(visitDTO.AppointmentID))
                return Conflict($"Appointment {visitDTO.AppointmentID} already has a visit.");

            if (!clsAppointment.IsExistByID(visitDTO.AppointmentID))
                return BadRequest("Invalid appointment id");

            if (clsAppointment.IsAppointmentCancelled(visitDTO.AppointmentID))
                return BadRequest("Appointment has been cancelled");

            if (visitDTO.CheckinTime is not DateTimeOffset checkinTime)
                return BadRequest("Checkin time is required");

            DateTime checkinTimeUtc = checkinTime.UtcDateTime;
            if (checkinTimeUtc > DateTime.UtcNow)
                return BadRequest("Checkin time cannot be in the future");

            clsVisit newVisit = new clsVisit();
            newVisit.AppointmentID = visitDTO.AppointmentID;
            newVisit.CheckinTime = checkinTimeUtc;
            newVisit.Notes = visitDTO.Notes;

            if (newVisit.Save())
                return CreatedAtRoute("GetVisitByID", new { id = newVisit.VisitID }, new VisitDTO
                {
                    Id = newVisit.VisitID,
                    AppointmentID = newVisit.AppointmentID,
                    CheckinTime = newVisit.CheckinTime,
                    CheckoutTime = newVisit.CheckoutTime,
                    Notes = newVisit.Notes,
                });

            return BadRequest("Invalid visit data");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id}", Name = "updatedVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateVisit([Range(1, int.MaxValue)] int id, UpdateVisitDTO visitDTO)
        {
            clsVisit? visit = clsVisit.Find(id);
            if (visit == null)
                return NotFound($"visit with id {id} not found");

            if (visit.IsLocked)
                return BadRequest("Visit has already been finalized");

            if (visitDTO.CheckinTime is not DateTimeOffset checkinTime)
                return BadRequest("Checkin time is required");

            DateTime checkinTimeUtc = checkinTime.UtcDateTime;

            if (visit.CheckoutTime.HasValue && checkinTimeUtc > visit.CheckoutTime.Value)
                return BadRequest("Checkin time cannot be greater than checkout time");
            else
            {
                if (checkinTimeUtc > DateTime.UtcNow)
                    return BadRequest("Checkin time cannot be in the future");
            }

            // Allow Admin to modify the resource
            if (visit.IsLocked && !User.IsInRole("Admin"))
                return BadRequest($"Visit is already locked");

            visit.CheckinTime = checkinTimeUtc;
            visit.Notes = visitDTO.Notes;

            if (visit.Save())
                return NoContent();

            return BadRequest("Invalid visit data");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPatch("{id}/checkout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult CheckoutVisit([Range(1, int.MaxValue)] int id)
        {
            clsVisit? visit = clsVisit.Find(id);
            if (visit == null)
                return NotFound($"visit with id {id} not found");

            clsDiagnosis? diagnosis = clsDiagnosis.FindByVisitID(id);
            if (diagnosis == null)
                return BadRequest("Visit must have a existing diagnosis");

            if (visit.IsLocked)
                return BadRequest("Visit is already locked");

            visit.CheckoutTime = DateTime.UtcNow;
            visit.IsLocked = true;

            if (visit.Save())
                return NoContent();

            return BadRequest("Invalid visit data");
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpDelete("{id}", Name = "DeleteVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteVisit([Range(1, int.MaxValue)] int id)
        {
            clsVisit? visit = clsVisit.Find(id);
            if (visit == null)
                return NotFound($"visit with id {id} not found");

            if (visit.IsLocked)
                return BadRequest("Visit has already been finalized");

            if (User.IsInRole("Staff"))
            {
                if (visit.IsLocked)
                    return BadRequest("Appointment is Locked");
            }

            if (clsVisit.Delete(id))
                return NoContent();

            return BadRequest("Invalid visit data");
        }

        private static DateTimeOffset AsUtc(DateTime stored) => new DateTimeOffset(DateTime.SpecifyKind(stored, DateTimeKind.Utc), TimeSpan.Zero);

        private static DateTimeOffset? AsUtc(DateTime? stored) => stored is DateTime d ? AsUtc(d) : null;
    }
}
