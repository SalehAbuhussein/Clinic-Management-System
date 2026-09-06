using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Diagnoses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Diagnoses")]
    [ApiController]
    public class DiagnosesController : ControllerBase
    {
        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet("{id}",Name = "GetDiagnosisByID")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<DiagnosisDTO> GetDiagnosisByID(int id)
        {
            if (id < 1)
                return BadRequest("Invalid Diagnosis ID");

            clsDiagnosis? diagnosis = clsDiagnosis.Find(id);
            if (diagnosis == null)
                return NotFound($"Diagnosis with id {id} not found");

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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<DiagnosisDTO> AddDiagnoses(CreateDiagnosesDTO createDiagnosesDTO)
        {
            if (createDiagnosesDTO.VisitID < 1)
                return BadRequest("Invalid visit id");

            if (clsDiagnosis.IsExistByVisitID(createDiagnosesDTO.VisitID))
                return Conflict($"Visit {createDiagnosesDTO.VisitID} already has a diagnosis");

            clsVisit? visit = clsVisit.Find(createDiagnosesDTO.VisitID);
            if (visit == null)
                return NotFound($"Visit with id {createDiagnosesDTO.VisitID} not found");

            if (visit.IsLocked)
                return BadRequest("Visit has been locked edits are not allowed");

            clsDiagnosis diagnosis = new clsDiagnosis();
            diagnosis.VisitID = createDiagnosesDTO.VisitID;
            diagnosis.Diagnosis = createDiagnosesDTO.Diagnosis;
            diagnosis.Notes = createDiagnosesDTO.Notes;

            if (diagnosis.Save())
                return CreatedAtRoute("GetDiagnosisByID", new { id = diagnosis.DiagnosesID }, createDiagnosesDTO);

            return BadRequest("Invalid Diagnosis data");
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpPut("{id}",Name = "UpdateDiagnosis")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateDiagnosis(int id, UpdateDiagnosesDTO updateDiagnosesDTO)
        {
            if (id < 1)
                return BadRequest("Invalid visit id");

            clsDiagnosis? diagnosis = clsDiagnosis.Find(id);
            if (diagnosis == null)
                return NotFound($"Diagnosis with id {id} not found");

            if (clsVisit.HasCheckedout(id))
                return BadRequest("Linked Visit has been already locked");

            diagnosis.Diagnosis = updateDiagnosesDTO.Diagnoses;
            diagnosis.Notes = updateDiagnosesDTO.Notes;

            if (diagnosis.Save())
                return Ok($"Diagnoses with id {id} updated successfully");

            return BadRequest("Invalid diagnosis data");
        }
    }
}
