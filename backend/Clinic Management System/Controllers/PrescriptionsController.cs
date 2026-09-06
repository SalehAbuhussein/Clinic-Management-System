using Clinic_Business;
using Clinic_DataAccess;
using Clinic_Management_System.DTOs.Models.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Prescriptions")]
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
            int visitID,
            int page = 1,
            int perPage = 10
        )
        {
            if (page < 1 || perPage < 1 || visitID < 1)
                return BadRequest("Invalid parameters");

            perPage = Math.Clamp(perPage, 1, 10);

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visitID,
                "ManagePrescription"
            );

            if (!authResult.Succeeded)
                return Forbid();

            DataTable items = clsPrescriptionItem.FindAllByVisitID(visitID, page, perPage);
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AddPrescriptionItems(
            CreatePrescriptionDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid Prescription data");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                request.VisitID,
                "ManagePrescription"
            );

            if (!authResult.Succeeded)
                return Forbid();

            // 1- Check if medicines list id are valid
            List<string> medicineIds = request.Items.Select(item => item.MedicineID.ToString()).ToList();
            if (!clsMedicine.IsValidMedicines(medicineIds))
                return BadRequest("Invalid medicine ids");

            // 2- If Visit has been checked out refuse.
            if (clsVisit.HasCheckedout(request.VisitID))
                return BadRequest("Invalid operation, Visit has already ended");

            // 3- If Visit has existing Prescription refuse (will be making endpoint to update).
            if (clsPrescriptionItem.HasPrescriptionByVisitID(request.VisitID))
                return BadRequest("Invalid operation, existing prescription exist");

            var items = request.Items.Select(item => (request.VisitID, item.MedicineID, item.Dosage, item.Frequency, item.Duration)).ToList();

            if (clsPrescriptionItem.CreatePrescriptionItems(items))
                return Ok("Prescription created successfully");

            return BadRequest("Invalid Prescription data");
        }

        [Authorize]
        [HttpPut(Name = "UpdatePrescriptionItems")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> UpdatePrescriptionItems(
            CreatePrescriptionDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid Prescription data");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                request.VisitID,
                "ManagePrescription"
            );

            if (!authResult.Succeeded)
                return Forbid();

            // 1- Check if medicines list id are valid
            List<string> medicineIds = request.Items.Select(item => item.MedicineID.ToString()).ToList();
            if (!clsMedicine.IsValidMedicines(medicineIds))
                return BadRequest("Invalid medicine ids");

            // 2- If Visit has been checked out refuse.
            if (clsVisit.HasCheckedout(request.VisitID))
                return BadRequest("Invalid operation, Visit has already ended");

            // 3- Make sure Prescription Exist.
            if (!clsPrescriptionItem.HasPrescriptionByVisitID(request.VisitID))
                return BadRequest("Invalid operation, No existing prescription");

            var items = request.Items.Select(item => (request.VisitID, item.MedicineID, item.Dosage, item.Frequency, item.Duration)).ToList();
            if (clsPrescriptionItem.UpdatePrescriptionItems(items) > 0)
                return Ok("Prescription updated successfully");

            return BadRequest("Invalid Prescription data");
        }
    }
}
