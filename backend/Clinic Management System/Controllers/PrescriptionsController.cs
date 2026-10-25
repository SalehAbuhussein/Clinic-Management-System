using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Clinic_Business.Entities.clsPrescriptionItem;

namespace Clinic_Management_System.Controllers
{
    // A prescription belongs to exactly one visit and a visit has at most one, so it is
    // a singular sub-resource rather than a top-level collection filtered by query.
    [Route("api/Visits/{visitId:int}/prescription")]
    [ApiController]
    public class PrescriptionsController : ControllerBase
    {
        private readonly IVisitRepository _visitRepository;
        private readonly IPrescriptionItemRepository _prescriptionItemRepository;
        private readonly IMedicineRepository _medicineRepository;
        private readonly ILogger<PrescriptionsController> _logger;

        public PrescriptionsController(
            IVisitRepository visitRepository,
            IPrescriptionItemRepository prescriptionItemRepository,
            IMedicineRepository medicineRepository,
            ILogger<PrescriptionsController> logger)
        {
            _visitRepository = visitRepository;
            _prescriptionItemRepository = prescriptionItemRepository;
            _medicineRepository = medicineRepository;
            _logger = logger;
        }

        [Authorize]
        [HttpGet(Name = "GetVisitPrescriptionItems")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<PrescriptionDTO>>> GetVisitPrescriptionItems(
            [FromServices] IAuthorizationService authorizationService,
            [Range(1, int.MaxValue)] int visitId
        )
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visitId,
                "ViewVisitRecord"
            );

            if (!authResult.Succeeded)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You do not have permission to do that.");

            PagedResult<PrescriptionItem> itemsPage =
                await _prescriptionItemRepository.FindAllByVisitIdAsync(visitId);

            List<PrescriptionDTO> prescriptions = itemsPage.Items.Select(item => new PrescriptionDTO
            {
                Id = item.Id,
                VisitID = item.VisitID,
                MedicineID = item.MedicineID,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Genericname = item.Genericname,
                Brandname = item.Brandname,
                Form = item.Form,
                Strength = item.Strength,
            }).ToList();

            return Ok(new PagedResult<PrescriptionDTO>(prescriptions, itemsPage.Page, itemsPage.PerPage, itemsPage.Total));
        }

        [Authorize]
        [HttpPost(Name = "AddPrescriptionItems")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> AddPrescriptionItems(
            int visitId,
            CreatePrescriptionDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visitId,
                "ManageVisitRecord"
            );

            if (!authResult.Succeeded)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You do not have permission to do that.");

            // 1- Check if medicines list id are valid
            List<string> medicineIds = request.Items.Select(item => item.MedicineID.ToString()).ToList();
            if (!await _medicineRepository.AreValidMedicinesAsync(medicineIds))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: "One or more of the medicines referenced does not exist.");

            // 2- If Visit has been checked out refuse.
            if (await _visitRepository.HasCheckedOutAsync(visitId))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit has already ended, so its prescription cannot be changed.");

            // 3- If Visit has existing Prescription refuse (will be making endpoint to update).
            if (await _prescriptionItemRepository.HasPrescriptionByVisitIdAsync(visitId))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "This visit already has a prescription.");

            var items = request.Items.Select(item => (visitId, item.MedicineID, item.Dosage, item.Frequency, item.Duration)).ToList();

            try
            {
                if (await _prescriptionItemRepository.CreateManyAsync(items))
                    return CreatedAtRoute("GetVisitPrescriptionItems", new { visitId }, null);
            }
            catch (SqlException exception) when (exception.Number is 2627 or 2601)
            {
                _logger.LogWarning(exception,
                    "Prescription raced past the guard for visit {VisitId}; the unique index refused it",
                    visitId);

                return Problem(statusCode: StatusCodes.Status409Conflict, detail: "Something went wrong. Please try again.");
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The prescription could not be saved.");
        }

        [Authorize]
        [HttpPut(Name = "UpdatePrescriptionItems")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdatePrescriptionItems(
            int visitId,
            CreatePrescriptionDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visitId,
                "ManageVisitRecord"
            );

            if (!authResult.Succeeded)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You do not have permission to do that.");

            // 1- Check if medicines list id are valid
            List<string> medicineIds = request.Items.Select(item => item.MedicineID.ToString()).ToList();
            if (!await _medicineRepository.AreValidMedicinesAsync(medicineIds))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: "One or more of the medicines referenced does not exist.");

            // 2- If Visit has been checked out refuse.
            if (await _visitRepository.HasCheckedOutAsync(visitId))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit has already ended, so its prescription cannot be changed.");

            // 3- Make sure Prescription Exist.
            if (!await _prescriptionItemRepository.HasPrescriptionByVisitIdAsync(visitId))
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: "This visit has no prescription to update.");

            var items = request.Items.Select(item => (visitId, item.MedicineID, item.Dosage, item.Frequency, item.Duration)).ToList();
            if (await _prescriptionItemRepository.UpdateManyAsync(items) > 0)
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The prescription could not be saved.");
        }
    }
}
