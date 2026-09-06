using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Doctors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Doctors")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetDoctors")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<DoctorDTO> GetDoctors([Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            perPage = Math.Clamp(perPage, 1, 10);

            DataTable table = clsDoctor.FindAllDoctors(page, perPage);
            List<DoctorDTO> peopleList = new List<DoctorDTO>();

            foreach (DataRow row in table.Rows)
            {
                peopleList.Add(new DoctorDTO
                {
                    Id = row.Field<int>("DoctorID"),
                    Specialization = row.Field<string>("Specialization") ?? string.Empty,
                    MedicalSchool = row.Field<string>("MedicalSchool") ?? string.Empty,
                    GraduationYear = checked((ushort)row.Field<short>("GraduationYear")),
                    PersonID = row.Field<int>("PersonID"),
                    Active = row.Field<bool>("Active")
                });
            }

            return Ok(peopleList);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetDoctorByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<DoctorDTO> GetDoctorByID([Range(1, int.MaxValue)] int id)
        {
            clsDoctor? doctor = clsDoctor.Find(id);
            if (doctor == null)
                return NotFound($"Doctor with id {id} not found");

            clsUser? user = doctor.UserInfo;
            if (user == null)
                return NotFound($"Doctor with id {id} not found");

            DoctorDTO doctorDTO = new DoctorDTO
            {
                Id = doctor.DoctorID,
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
        public ActionResult<DoctorDTO> AddDoctor(CreateDoctorDTO addDoctorDTO)
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

            if (newDoctor.Save())
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

            return BadRequest("Invalid doctor data.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}", Name = "UpdateDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult UpdateDoctor(int id, UpdateDoctorDTO updatedDoctor)
        {
            clsDoctor? doctor = clsDoctor.Find(id);
            if (doctor == null)
                return NotFound($"Doctor with id {id} not found");

            clsPerson? person = doctor.UserInfo?.PersonInfo; 
            if (person == null)
                return BadRequest("Invalid doctor data");

            clsUser? user = doctor.UserInfo;
            if (user != null && !user.Active)
                return NotFound($"Doctor with id {id} not found");

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

            if (doctor.Save())
                return NoContent();

            return BadRequest("Invalid doctor data");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeleteDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult DeleteDoctor([Range(1, int.MaxValue)] int id)
        {
            clsDoctor? doctor = clsDoctor.Find(id);
            if (doctor == null)
                return NotFound($"doctor with id {id} not found");

            clsUser? user = doctor.UserInfo;
            if (user != null && !user.Active)
                return NotFound($"doctor with id {id} not found");

            if (clsDoctor.DeleteDoctor(id))
                return NoContent();

            return BadRequest("Invalid doctor data");
        }
    }
}
