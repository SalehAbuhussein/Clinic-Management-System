using Clinic_Business;
using Clinic_Management_System.DTOs.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Clinic_Management_System.Controllers
{
    [Route("api/People")]
    [ApiController]
    public class PeopleController : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetPeople")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<List<PersonDTO>> GetPeople(int page = 1, int perPage = 10)
        {
            if (page < 1 || perPage < 1)
                return BadRequest("Invalid parameters");

            perPage = Math.Clamp(perPage, 1, 10);

            DataTable table = clsPerson.FindAll(page, perPage);
            List<PersonDTO> peopleList = new List<PersonDTO>();

            foreach (DataRow row in table.Rows)
            {
                peopleList.Add(new PersonDTO
                {
                    Id = row.Field<int>("PersonID"),
                    Firstname = row.Field<string>("Firstname") ?? string.Empty,
                    Midname = row.Field<string>("Midname") ?? string.Empty,
                    Thirdname = row.Field<string>("Thirdname"),
                    Lastname = row.Field<string>("Lastname") ?? string.Empty,
                    Phone = row.Field<string>("Phone") ?? string.Empty,
                    Gender = row.Field<bool>("Gender"),
                    DateOfBirth = row.Field<DateTime>("DateOfBirth"),
                    ProfileImage = row.Field<string>("ProfileImage")
                });
            }

            return Ok(peopleList);
        }

        [Authorize]
        [HttpGet("{id}", Name = "GetPersonById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PersonDTO>> GetPersonById(
            int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            if (id < 1)
                return BadRequest("Invalid person id.");

            clsPerson? person = clsPerson.Find(id);

            if (person == null)
                return NotFound($"Person with id {id} not found.");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                id,
                "PersonOwnerOrAuthorized"
            );

            if (!authResult.Succeeded)
                return Forbid();

            PersonDTO personDTO = new PersonDTO
            {
                Id = id,
                Firstname = person.Firstname,
                Midname = person.Midname,
                Lastname = person.Lastname,
                Thirdname = person.Thirdname,
                Gender = person.Gender,
                Phone = person.Phone,
                DateOfBirth = person.DateOfBirth,
                ProfileImage = person.ProfileImage
            };

            return Ok(personDTO);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeletePerson")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult DeletePerson(int id)
        {
            if (id < 1)
                return BadRequest("Invalid person id.");

            clsPerson? person = clsPerson.Find(id);
            if (person == null)
                return NotFound($"Person with ID {id} not found.");

            bool isDeleted = clsPerson.Delete(id);

            if (isDeleted)
                return Ok($"Person with id {id} has been deleted.");

            return BadRequest($"Something went wrong with deletion of person {id}");
        }
    }
}
