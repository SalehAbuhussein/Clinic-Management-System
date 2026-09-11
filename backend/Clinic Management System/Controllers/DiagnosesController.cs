using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Diagnoses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Controllers
{
    // A diagnosis belongs to exactly one visit and a visit has at most one, so it is
    // a singular sub-resource rather than a top-level collection — the same shape as
    // the prescription on the same parent.
    [Route("api/Visits/{visitId:int}/diagnosis")]
    [ApiController]
    public class DiagnosesController : ControllerBase
    {
        private readonly IVisitRepository _visitRepository;
        private readonly IDiagnosisRepository _diagnosisRepository;
        private readonly IAuthorizationService _authorizationService;

        public DiagnosesController(
            IVisitRepository visitRepository, 
            IDiagnosisRepository diagnosisRepository,
            IAuthorizationService authorizationService)
        {
            _visitRepository = visitRepository;
            _diagnosisRepository = diagnosisRepository;
            _authorizationService = authorizationService;
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet(Name = "GetVisitDiagnosis")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DiagnosisDTO>> GetVisitDiagnosis([Range(1, int.MaxValue)] int visitId)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, visitId, "ManagePrescription");

            if (!authResult.Succeeded)
                return Forbid();

            clsDiagnosis? diagnosis = await _diagnosisRepository.FindByVisitIdAsync(visitId);
            if (diagnosis == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit {visitId} has no diagnosis");

            return new DiagnosisDTO
            {
                DiagnosisID = diagnosis.DiagnosesID ?? 0,
                Diagnosis = diagnosis.Diagnosis,
                Notes = diagnosis.Notes
            };
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpPost(Name = "AddVisitDiagnosis")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<DiagnosisDTO>> AddVisitDiagnosis(
            [Range(1, int.MaxValue)] int visitId,
            CreateDiagnosesDTO createDiagnosesDTO)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, visitId, "ManagePrescription");

            if (!authResult.Succeeded)
                return Forbid();

            if (await _diagnosisRepository.ExistsByVisitIdAsync(visitId))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: $"Visit {visitId} already has a diagnosis");

            clsVisit? visit = await _visitRepository.FindByIdAsync(visitId);
            if (visit == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit with id {visitId} not found");

            if (visit.IsLocked)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit is locked, so its diagnosis cannot be changed.");

            clsDiagnosis diagnosis = new clsDiagnosis();
            diagnosis.VisitID = visitId;
            diagnosis.Diagnosis = createDiagnosesDTO.Diagnosis;
            diagnosis.Notes = createDiagnosesDTO.Notes;

            if (await diagnosis.SaveAsync())
                return CreatedAtRoute("GetVisitDiagnosis", new { visitId },
                    new DiagnosisDTO
                    {
                        DiagnosisID = diagnosis.DiagnosesID ?? 0,
                        Diagnosis = diagnosis.Diagnosis,
                        Notes = diagnosis.Notes
                    });

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The diagnosis could not be saved. Check the diagnosis text.");
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpPut(Name = "UpdateVisitDiagnosis")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateVisitDiagnosis(
            [Range(1, int.MaxValue)] int visitId,
            UpdateDiagnosesDTO updateDiagnosesDTO)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, visitId, "ManagePrescription");

            if (!authResult.Succeeded)
                return Forbid();

            clsDiagnosis? diagnosis = await _diagnosisRepository.FindByVisitIdAsync(visitId);
            if (diagnosis == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit {visitId} has no diagnosis");

            if (await _visitRepository.HasCheckedOutAsync(visitId))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit is locked, so its diagnosis cannot be changed.");

            diagnosis.Diagnosis = updateDiagnosesDTO.Diagnosis;
            diagnosis.Notes = updateDiagnosesDTO.Notes;

            if (await diagnosis.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The diagnosis could not be saved. Check the diagnosis text.");
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpDelete(Name = "DeleteVisitDiagnosis")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeleteVisitDiagnosis([Range(1, int.MaxValue)] int visitId)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, visitId, "ManagePrescription");

            if (!authResult.Succeeded)
                return Forbid();

            clsDiagnosis? diagnosis = await _diagnosisRepository.FindByVisitIdAsync(visitId);
            if (diagnosis == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit {visitId} has no diagnosis");

            if (await _visitRepository.HasCheckedOutAsync(visitId))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit is locked, so its diagnosis cannot be removed.");

            if (await _diagnosisRepository.DeleteAsync(diagnosis.DiagnosesID ?? 0))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The diagnosis could not be deleted.");
        }
    }
}
