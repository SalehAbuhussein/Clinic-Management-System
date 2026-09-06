using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Patients")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet(Name = "GetPatients")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<PatientDTO> GetPatients([Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            perPage = Math.Clamp(perPage, 1, 10);

            DataTable table = clsPatient.FindPatients(page, perPage);
            List<PatientDTO> patientsList = new List<PatientDTO>();

            foreach (DataRow row in table.Rows)
            {
                patientsList.Add(new PatientDTO
                {
                    Id = row.Field<int>("PatientID"),
                    EmergencyContactName = row.Field<string>("EmergencyContactName"),
                    EmergencyContactNumber = row.Field<string>("EmergencyContactNumber"),
                    PersonID = row.Field<int>("PersonID")
                });
            }

            return Ok(patientsList);
        }

        [Authorize(Roles = "Admin,Staff,Doctor")]
        [HttpGet("{id}", Name = "GetPatientByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<PatientDTO> GetPatientByID([Range(1, int.MaxValue)] int id)
        {
            clsPatient? patient = clsPatient.FindPatient(id);
            if (patient == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Patient with id {id} not found");

            PatientDTO patientDTO = new PatientDTO
            {
                Id = id,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactNumber = patient.EmergencyContactNumber,
                PersonID = patient.PersonID
            };

            return Ok(patientDTO);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost(Name = "AddPatient")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult<PatientDTO> AddPatient(CreatePatientDTO patientDTO)
        {
            if (clsPatient.FindPatientByNationalNo(patientDTO.NationalNo) != null)
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "A patient with this national number already exists.");

            clsPatient newPatient = new clsPatient();
            newPatient.EmergencyContactNumber = patientDTO.EmergencyContactNumber;
            newPatient.EmergencyContactName = patientDTO.EmergencyContactName;
            newPatient.CreatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            newPatient.Firstname = patientDTO.Firstname;
            newPatient.Midname = patientDTO.Midname;
            newPatient.Thirdname = patientDTO.Thirdname;
            newPatient.Lastname = patientDTO.Lastname;
            newPatient.NationalNo = patientDTO.NationalNo;
            newPatient.Phone = patientDTO.Phone;
            newPatient.Gender = patientDTO.Gender!.Value;
            newPatient.DateOfBirth = patientDTO.DateOfBirth!.Value;

            if (newPatient.Save())
            {
                PatientDTO newPatientDTO = new PatientDTO
                {
                    Id = newPatient.PatientID,
                    EmergencyContactName = newPatient.EmergencyContactName,
                    EmergencyContactNumber = newPatient.EmergencyContactNumber,
                    PersonID = newPatient.PersonID,
                };
                return CreatedAtRoute("GetPatientByID", new { id = newPatient.PatientID }, newPatientDTO);
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The patient could not be saved. Check the name, phone, national number and date of birth.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id}", Name = "UpdatePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult UpdatePatient(int id, UpdatePatientDTO updatedPatient)
        {
            clsPatient? patient = clsPatient.FindPatient(id);

            if (patient == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Patient with id {id} not found");

            patient.EmergencyContactNumber = updatedPatient.EmergencyContactNumber;
            patient.EmergencyContactName = updatedPatient.EmergencyContactName;
            patient.Firstname = updatedPatient.Firstname;
            patient.Midname = updatedPatient.Midname;
            patient.Thirdname = updatedPatient.Thirdname;
            patient.Lastname = updatedPatient.Lastname;
            patient.Phone = updatedPatient.Phone;
            patient.Gender = updatedPatient.Gender!.Value;
            patient.DateOfBirth = updatedPatient.DateOfBirth!.Value;

            if (patient.Save())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The patient could not be saved. Check the name, phone, national number and date of birth.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeletePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult DeletePatient([Range(1, int.MaxValue)] int id)
        {
            clsPatient? patient = clsPatient.FindPatient(id);
            if (patient == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Patient with id {id} not found");

            if (clsPatient.DeletePatient(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The patient could not be deleted.");
        }
    }
}
