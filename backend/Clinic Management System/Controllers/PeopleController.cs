using Clinic_Business;
using Clinic_Management_System.DTOs.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<List<PersonDTO>> GetPeople([Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonDTO>> GetPersonById(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsPerson? person = clsPerson.Find(id);

            if (person == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Person with id {id} not found");

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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult DeletePerson([Range(1, int.MaxValue)] int id)
        {
            clsPerson? person = clsPerson.Find(id);
            if (person == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Person with id {id} not found");

            if (clsPerson.Delete(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The person could not be deleted.");
        }
    }
}
