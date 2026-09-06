using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Clinic_Management_System.Controllers
{
    // A prescription belongs to exactly one visit and a visit has at most one, so it is
    // a singular sub-resource rather than a top-level collection filtered by query.
    [Route("api/Visits/{visitId:int}/prescription")]
    [ApiController]
    public class PrescriptionsController : ControllerBase
    {
        [Authorize]
        [HttpGet(Name = "GetVisitPrescriptionItems")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<PrescriptionDTO>>> GetVisitPrescriptionItems(
            [FromServices] IAuthorizationService authorizationService,
            [Range(1, int.MaxValue)] int visitId,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 10)] int perPage = 10
        )
        {
            perPage = Math.Clamp(perPage, 1, 10);

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visitId,
                "ManagePrescription"
            );

            if (!authResult.Succeeded)
                return Forbid();

            DataTable items = clsPrescriptionItem.FindAllByVisitID(visitId, page, perPage);
            List<PrescriptionDTO> prescriptions = new List<PrescriptionDTO>();

            foreach (DataRow row in items.Rows)
            {
                prescriptions.Add(new PrescriptionDTO
                {
                    Id = row.Field<int>("PrescriptionItemID"),
                    VisitID = row.Field<int>("VisitID"),
                    MedicineID = row.Field<int>("MedicineID"),
                    Dosage = row.Field<string>("Dosage") ?? string.Empty,
                    Frequency = row.Field<string>("Frequency") ?? string.Empty,
                    Duration = row.Field<string>("Duration") ?? string.Empty,
                });
            }

            return Ok(prescriptions);
        }

        [Authorize]
        [HttpPost(Name = "AddPrescriptionItems")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AddPrescriptionItems(
            int visitId,
            CreatePrescriptionDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visitId,
                "ManagePrescription"
            );

            if (!authResult.Succeeded)
                return Forbid();

            // 1- Check if medicines list id are valid
            List<string> medicineIds = request.Items.Select(item => item.MedicineID.ToString()).ToList();
            if (!clsMedicine.IsValidMedicines(medicineIds))
                return BadRequest("Invalid medicine ids");

            // 2- If Visit has been checked out refuse.
            if (clsVisit.HasCheckedout(visitId))
                return BadRequest("Invalid operation, Visit has already ended");

            // 3- If Visit has existing Prescription refuse (will be making endpoint to update).
            if (clsPrescriptionItem.HasPrescriptionByVisitID(visitId))
                return BadRequest("Invalid operation, existing prescription exist");

            var items = request.Items.Select(item => (visitId, item.MedicineID, item.Dosage, item.Frequency, item.Duration)).ToList();

            if (clsPrescriptionItem.CreatePrescriptionItems(items))
                return CreatedAtRoute("GetVisitPrescriptionItems", new { visitId }, null);

            return BadRequest("Invalid Prescription data");
        }

        [Authorize]
        [HttpPut(Name = "UpdatePrescriptionItems")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> UpdatePrescriptionItems(
            int visitId,
            CreatePrescriptionDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visitId,
                "ManagePrescription"
            );

            if (!authResult.Succeeded)
                return Forbid();

            // 1- Check if medicines list id are valid
            List<string> medicineIds = request.Items.Select(item => item.MedicineID.ToString()).ToList();
            if (!clsMedicine.IsValidMedicines(medicineIds))
                return BadRequest("Invalid medicine ids");

            // 2- If Visit has been checked out refuse.
            if (clsVisit.HasCheckedout(visitId))
                return BadRequest("Invalid operation, Visit has already ended");

            // 3- Make sure Prescription Exist.
            if (!clsPrescriptionItem.HasPrescriptionByVisitID(visitId))
                return BadRequest("Invalid operation, No existing prescription");

            var items = request.Items.Select(item => (visitId, item.MedicineID, item.Dosage, item.Frequency, item.Duration)).ToList();
            if (clsPrescriptionItem.UpdatePrescriptionItems(items) > 0)
                return NoContent();

            return BadRequest("Invalid Prescription data");
        }
    }
}
