using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Patients;
using Clinic_Management_System.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Patients")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientRepository _patientRepository;

        public PatientsController(IPatientRepository patientRepository)
        {
            _patientRepository = patientRepository;
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet(Name = "GetPatients")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<PatientDTO>>> GetPatients(string? fullname, string? phone, [Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            perPage = Math.Clamp(perPage, 1, 10);

            PagedResult<clsPatient.PatientItem> patientsPage = await _patientRepository.FindAllAsync(fullname, phone, null, null, page, perPage);

            List<PatientDTO> patientsList = patientsPage.Items
                .Select(patient => new PatientDTO
                {
                    Id = patient.Id,
                    Fullname = patient.Fullname,
                    Phone = patient.Phone,
                    EmergencyContactName = patient.EmergencyContactName,
                    EmergencyContactNumber = patient.EmergencyContactNumber,
                    PersonID = patient.PersonID
                }).ToList();

            return Ok(new PagedResult<PatientDTO>(patientsList, patientsPage.Page, patientsPage.PerPage, patientsPage.Total));
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("export", Name = "ExportPatients")]
        [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, ExcelFile.SpreadsheetContentType)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ExportPatients(string? fullname, string? phone, DateTimeOffset? from, DateTimeOffset? to)
        {
            if (await _patientRepository.GetTotalCountAsync(fullname, phone, from, to) > ExcelFile.MaxRows)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"That search matches more than {ExcelFile.MaxRows:N0} patients. Narrow it and try again.");

            PagedResult<clsPatient.PatientItem> patientsPage = await _patientRepository.FindAllAsync(fullname, phone, from, to, 1, ExcelFile.MaxRows);

            return ExcelFile.Build("Patients", ExportColumns, patientsPage.Items);
        }

        private static readonly ExcelColumn<clsPatient.PatientItem>[] ExportColumns =
        [
            new("Patient", p => p.Fullname),
            new("Phone", p => p.Phone),
            new("Emergency contact", p => p.EmergencyContactName),
            new("Emergency number", p => p.EmergencyContactNumber),
            new("Registered", p => p.RegisteredAt),
        ];

        [Authorize(Roles = "Admin,Staff,Doctor")]
        [HttpGet("{id}", Name = "GetPatientByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientDTO>> GetPatientByID(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsPatient? patient = await _patientRepository.FindByIdAsync(id);
            var authorization = await authorizationService.AuthorizeAsync(User, id, "ViewPatient");
            if (patient == null || !authorization.Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Patient with id {id} not found");

            PatientDTO patientDTO = new PatientDTO
            {
                Id = id,
                Fullname = patient.Fullname,
                Firstname = patient.Firstname,
                Midname = patient.Midname,
                Thirdname = patient.Thirdname,
                Lastname = patient.Lastname,
                NationalNo = patient.NationalNo,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                Phone = patient.Phone,
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
        public async Task<ActionResult<PatientDTO>> AddPatient(CreatePatientDTO patientDTO)
        {
            if (await _patientRepository.FindByNationalNoAsync(patientDTO.NationalNo) != null)
                return Problem(statusCode: StatusCodes.Status409Conflict, detail: "A patient with this national number already exists.");

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

            if (await newPatient.SaveAsync())
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

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The patient could not be saved. Check the name, phone, national number and date of birth.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id}", Name = "UpdatePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdatePatient(int id, UpdatePatientDTO updatedPatient)
        {
            clsPatient? patient = await _patientRepository.FindByIdAsync(id);

            if (patient == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Patient with id {id} not found");

            patient.EmergencyContactNumber = updatedPatient.EmergencyContactNumber;
            patient.EmergencyContactName = updatedPatient.EmergencyContactName;
            patient.Firstname = updatedPatient.Firstname;
            patient.Midname = updatedPatient.Midname;
            patient.Thirdname = updatedPatient.Thirdname;
            patient.Lastname = updatedPatient.Lastname;
            patient.Phone = updatedPatient.Phone;
            patient.Gender = updatedPatient.Gender!.Value;
            patient.DateOfBirth = updatedPatient.DateOfBirth!.Value;

            if (await patient.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The patient could not be saved. Check the name, phone, national number and date of birth.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeletePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeletePatient([Range(1, int.MaxValue)] int id)
        {
            clsPatient? patient = await _patientRepository.FindByIdAsync(id);
            if (patient == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Patient with id {id} not found");

            if (await _patientRepository.DeleteAsync(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The patient could not be deleted.");
        }
    }
}
