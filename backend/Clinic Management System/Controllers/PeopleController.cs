using Clinic_Business.Common;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.People;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Clinic_Business.Entities;

namespace Clinic_Management_System.Controllers
{
    [Route("api/People")]
    [ApiController]
    public class PeopleController : ControllerBase
    {
        private readonly IPersonRepository _personRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfileImageStore _profileImageStore;

        public PeopleController(IPersonRepository personRepository, IUserRepository userRepository, IProfileImageStore profileImageStore)
        {
            _personRepository = personRepository;
            _userRepository = userRepository;
            _profileImageStore = profileImageStore;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetPeople")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<PersonDTO>>> GetPeople(string? fullname, string? nationalNo, string? phone, [Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            PagedResult<clsPerson> peoplePage = await _personRepository.FindAllAsync(fullname, nationalNo, phone, page, perPage);

            List<PersonDTO> peopleList = peoplePage.Items.Select(person => new PersonDTO
            {
                Id = person.PersonID ?? 0,
                Firstname = person.Firstname,
                Midname = person.Midname,
                Thirdname = person.Thirdname,
                Lastname = person.Lastname,
                NationalNo = person.NationalNo,
                Phone = person.Phone,
                Gender = person.Gender,
                DateOfBirth = person.DateOfBirth,
                ProfileImage = person.ProfileImage
            }).ToList();

            return Ok(new PagedResult<PersonDTO>(peopleList, peoplePage.Page, peoplePage.PerPage, peoplePage.Total));
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
            clsPerson? person = await _personRepository.FindByIdAsync(id);

            if (person == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Person with id {id} not found");

            var authResult = await authorizationService.AuthorizeAsync(
                User,
                id,
                "PersonOwnerOrAuthorized"
            );

            if (!authResult.Succeeded)
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You do not have permission to do that.");

            PersonDTO personDTO = new PersonDTO
            {
                Id = id,
                Firstname = person.Firstname,
                Midname = person.Midname,
                Lastname = person.Lastname,
                Thirdname = person.Thirdname,
                NationalNo = person.NationalNo,
                Gender = person.Gender,
                Phone = person.Phone,
                DateOfBirth = person.DateOfBirth,
                ProfileImage = person.ProfileImage
            };

            return Ok(personDTO);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}", Name = "UpdatePerson")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdatePerson(
            [Range(1, int.MaxValue)] int id,
            UpdatePersonDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsPerson? person = await _FindEditablePersonAsync(id, authorizationService);

            if (person == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Person with id {id} not found!");

            person.Firstname = request.Firstname;
            person.Midname = request.Midname;
            person.Thirdname = request.Thirdname;
            person.Lastname = request.Lastname;
            person.Phone = request.Phone;
            person.Gender = request.Gender!.Value;
            person.DateOfBirth = request.DateOfBirth!.Value;

            if (!person.IsValid())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "Check the name, phone and date of birth.");

            if (await _personRepository.UpdateAsync(person))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The person could not be saved.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/image", Name = "UploadPersonImage")]
        [RequestSizeLimit(ProfileImageStore.MaxBytes)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UploadPersonImage(
            [Range(1, int.MaxValue)] int id,
            IFormFile image,
            [FromServices] IAuthorizationService authorizationService,
            CancellationToken cancellationToken
        )
        {
            clsPerson? person = await _FindEditablePersonAsync(id, authorizationService);

            if (person == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Person with id {id} not found!");

            string? newFileName = await _profileImageStore.SaveAsync(image, cancellationToken);

            if (newFileName == null)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "That file is not valid image file.");

            string? oldFileName = person.ProfileImage;

            person.ProfileImage = newFileName;

            if (!await _personRepository.UpdateAsync(person))
            {
                _profileImageStore.Delete(newFileName);
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The picture could not be saved.");
            }

            _profileImageStore.Delete(oldFileName);

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}/image", Name = "RemovePersonImage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> RemovePersonImage(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsPerson? person = await _FindEditablePersonAsync(id, authorizationService);

            if (person == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Person with id {id} not found!");

            string? oldFileName = person.ProfileImage;

            if (oldFileName == null)
                return NoContent();

            person.ProfileImage = null;

            if (!await _personRepository.UpdateAsync(person))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The picture could not be removed.");

            _profileImageStore.Delete(oldFileName);

            return NoContent();
        }

        /// <summary>
        /// The person this admin is allowed to write to.
        ///
        /// A person who owns an account is edited through that account's rules,
        /// which is what stops one admin from rewriting another admin's details.
        /// A person with no account belongs to the clinic's records and any admin
        /// may correct it.
        /// </summary>
        /// <returns>The person, or null when there is none or the caller cannot touch it.</returns>
        private async Task<clsPerson?> _FindEditablePersonAsync(int id, IAuthorizationService authorizationService)
        {
            clsPerson? person = await _personRepository.FindByIdAsync(id);
            if (person == null)
                return null;

            clsUser? owner = await _userRepository.FindByPersonIDAsync(id);
            if (owner != null && !(await authorizationService.AuthorizeAsync(User, owner, "ManageAccount")).Succeeded)
                return null;

            return person;
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeletePerson")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeletePerson([Range(1, int.MaxValue)] int id)
        {
            clsPerson? person = await _personRepository.FindByIdAsync(id);
            if (person == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Person with id {id} not found!");

            if (await _personRepository.IsUsedAsync(id))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "This person belongs to a user or a patient and cannot be deleted.");

            if (await _personRepository.DeleteAsync(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The person could not be deleted.");
        }
    }
}
