using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Doctors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Doctors")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorRepository _doctorRepository;

        public DoctorsController(IDoctorRepository doctorRepository)
        {
            _doctorRepository = doctorRepository;
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet(Name = "GetDoctors")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<DoctorDTO>>> GetDoctors(string? fullname, string? specialization, bool? active, [Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            perPage = Math.Clamp(perPage, 1, 10);

            PagedResult<clsDoctor.DoctorItem> doctorsPage = await _doctorRepository.FindAllAsync(fullname, specialization, active, page, perPage);

            List<DoctorDTO> doctorsList = doctorsPage.Items
                .Select(doctor => new DoctorDTO
                {
                    Id = doctor.Id,
                    Fullname = doctor.Fullname,
                    Firstname = doctor.Firstname,
                    Midname = doctor.Midname,
                    Thirdname = doctor.Thirdname,
                    Lastname = doctor.Lastname,
                    NationalNo = doctor.NationalNo,
                    Phone = doctor.Phone,
                    Gender = doctor.Gender,
                    DateOfBirth = doctor.DateOfBirth,
                    Specialization = doctor.Specialization,
                    MedicalSchool = doctor.MedicalSchool,
                    GraduationYear = doctor.GraduationYear,
                    PersonID = doctor.PersonID,
                    Active = doctor.Active
                }).ToList();

            return Ok(new PagedResult<DoctorDTO>(doctorsList, doctorsPage.Page, doctorsPage.PerPage, doctorsPage.Total));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetDoctorByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorDTO>> GetDoctorByID([Range(1, int.MaxValue)] int id)
        {
            clsDoctor? doctor = await _doctorRepository.FindByIdAsync(id);
            if (doctor == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Doctor with id {id} not found");

            clsUser? user = doctor.UserInfo;
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Doctor with id {id} not found");

            clsPerson person = user.PersonInfo;

            DoctorDTO doctorDTO = new DoctorDTO
            {
                Id = doctor.DoctorID,
                Fullname = person.Fullname,
                Firstname = person.Firstname,
                Midname = person.Midname,
                Thirdname = person.Thirdname,
                Lastname = person.Lastname,
                NationalNo = person.NationalNo,
                Phone = person.Phone,
                Gender = person.Gender,
                DateOfBirth = person.DateOfBirth,
                MedicalSchool = doctor.MedicalSchool,
                Specialization = doctor.Specialization,
                GraduationYear = doctor.GraduationYear,
                PersonID = user.PersonID ?? 0,
                Active = user.Active
            };

            return Ok(doctorDTO);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost(Name = "AddDoctor")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<DoctorDTO>> AddDoctor(CreateDoctorDTO addDoctorDTO)
        {
            clsDoctor newDoctor = new clsDoctor();
            newDoctor.Specialization = addDoctorDTO.Specialization;
            newDoctor.MedicalSchool = addDoctorDTO.MedicalSchool;
            newDoctor.GraduationYear = addDoctorDTO.GraduationYear;
            newDoctor.CreatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            newDoctor.UserInfo.PersonInfo.Firstname = addDoctorDTO.Firstname;
            newDoctor.UserInfo.PersonInfo.Midname = addDoctorDTO.Midname;
            newDoctor.UserInfo.PersonInfo.Thirdname = addDoctorDTO.Thirdname;
            newDoctor.UserInfo.PersonInfo.Lastname = addDoctorDTO.Lastname;
            newDoctor.UserInfo.PersonInfo.Phone = addDoctorDTO.Phone;
            newDoctor.UserInfo.PersonInfo.Gender = addDoctorDTO.Gender!.Value;
            newDoctor.UserInfo.PersonInfo.DateOfBirth = addDoctorDTO.DateOfBirth!.Value;
            newDoctor.UserInfo.PersonInfo.NationalNo = addDoctorDTO.NationalNo;
            newDoctor.UserInfo.PersonInfo.ProfileImage = addDoctorDTO.ProfileImage;
            newDoctor.UserInfo.Email = addDoctorDTO.Email;
            newDoctor.UserInfo.PasswordHash = BCrypt.Net.BCrypt.HashPassword(addDoctorDTO.Password);

            if (await newDoctor.SaveAsync())
            {
                DoctorDTO newDoctorDTO = new DoctorDTO
                {
                    Id = newDoctor.DoctorID,
                    Specialization = addDoctorDTO.Specialization,
                    MedicalSchool = addDoctorDTO.MedicalSchool,
                    GraduationYear = addDoctorDTO.GraduationYear,
                    PersonID = newDoctor.UserInfo.PersonID ?? 0
                };
                return CreatedAtRoute("GetDoctorByID", new { id = newDoctor.DoctorID }, newDoctorDTO);
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The doctor could not be created. Check the person details and credentials.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}", Name = "UpdateDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateDoctor(int id, UpdateDoctorDTO updatedDoctor)
        {
            clsDoctor? doctor = await _doctorRepository.FindByIdAsync(id);
            if (doctor == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Doctor with id {id} not found");

            clsPerson? person = doctor.UserInfo?.PersonInfo;
            if (person == null)
                return Problem(statusCode: StatusCodes.Status500InternalServerError,
                    detail: $"Doctor {id} is missing its linked person record.");

            clsUser? user = doctor.UserInfo;
            if (user != null && !user.Active)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Doctor with id {id} not found");

            doctor.Specialization = updatedDoctor.Specialization;
            doctor.MedicalSchool = updatedDoctor.MedicalSchool;
            doctor.GraduationYear = updatedDoctor.GraduationYear;

            person.Firstname = updatedDoctor.Firstname;
            person.Midname = updatedDoctor.Midname;
            person.Thirdname = updatedDoctor.Thirdname;
            person.Lastname = updatedDoctor.Lastname;
            person.Phone = updatedDoctor.Phone;
            person.Gender = updatedDoctor.Gender!.Value;
            person.DateOfBirth = updatedDoctor.DateOfBirth!.Value;
            person.ProfileImage = updatedDoctor.ProfileImage;

            if (await doctor.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The doctor could not be saved. Check the person details and specialization.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeleteDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeleteDoctor([Range(1, int.MaxValue)] int id)
        {
            clsDoctor? doctor = await _doctorRepository.FindByIdAsync(id);
            if (doctor == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Doctor with id {id} not found");

            clsUser? user = doctor.UserInfo;
            if (user != null && !user.Active)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Doctor with id {id} not found");

            if (await _doctorRepository.DeleteAsync(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The doctor could not be deleted.");
        }
    }
}
