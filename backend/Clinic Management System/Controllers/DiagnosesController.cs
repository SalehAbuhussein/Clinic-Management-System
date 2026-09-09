using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Diagnoses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Diagnoses")]
    [ApiController]
    public class DiagnosesController : ControllerBase
    {
        private readonly IVisitRepository _visitRepository;
        private readonly IDiagnosisRepository _diagnosisRepository;

        public DiagnosesController(IVisitRepository visitRepository, IDiagnosisRepository diagnosisRepository)
        {
            _visitRepository = visitRepository;
            _diagnosisRepository = diagnosisRepository;
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet("{id}",Name = "GetDiagnosisByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DiagnosisDTO>> GetDiagnosisByID([Range(1, int.MaxValue)] int id)
        {
            clsDiagnosis? diagnosis = await _diagnosisRepository.FindByIdAsync(id);
            if (diagnosis == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Diagnosis with id {id} not found");

            return new DiagnosisDTO
            {
                DiagnosisID = id,
                Diagnosis = diagnosis.Diagnosis,
                Notes = diagnosis.Notes
            };
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpPost(Name = "AddDiagnoses")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<DiagnosisDTO>> AddDiagnoses(CreateDiagnosesDTO createDiagnosesDTO)
        {
            if (await _diagnosisRepository.ExistsByVisitIdAsync(createDiagnosesDTO.VisitID))
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: $"Visit {createDiagnosesDTO.VisitID} already has a diagnosis");

            clsVisit? visit = await _visitRepository.FindByIdAsync(createDiagnosesDTO.VisitID);
            if (visit == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Visit with id {createDiagnosesDTO.VisitID} not found");

            if (visit.IsLocked)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit is locked, so its diagnosis cannot be changed.");

            clsDiagnosis diagnosis = new clsDiagnosis();
            diagnosis.VisitID = createDiagnosesDTO.VisitID;
            diagnosis.Diagnosis = createDiagnosesDTO.Diagnosis;
            diagnosis.Notes = createDiagnosesDTO.Notes;

            if (await diagnosis.SaveAsync())
                return CreatedAtRoute("GetDiagnosisByID", new { id = diagnosis.DiagnosesID },
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
        [HttpPut("{id}",Name = "UpdateDiagnosis")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateDiagnosis([Range(1, int.MaxValue)] int id, UpdateDiagnosesDTO updateDiagnosesDTO)
        {
            clsDiagnosis? diagnosis = await _diagnosisRepository.FindByIdAsync(id);
            if (diagnosis == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Diagnosis with id {id} not found");

            if (await _visitRepository.HasCheckedOutAsync(diagnosis.VisitID))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "The visit is locked, so its diagnosis cannot be changed.");

            diagnosis.Diagnosis = updateDiagnosesDTO.Diagnosis;
            diagnosis.Notes = updateDiagnosesDTO.Notes;

            if (await diagnosis.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The diagnosis could not be saved. Check the diagnosis text.");
        }
    }
}
