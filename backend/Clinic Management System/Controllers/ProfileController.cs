using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.People;
using Clinic_Management_System.DTOs.Models.Profile;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IPersonRepository _personRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfileImageStore _profielImageStore;

        public ProfileController(
            IPersonRepository personRepository,
            IUserRepository userRepository,
            IProfileImageStore profielImageStore
        )
        {
            _personRepository = personRepository;
            _userRepository = userRepository;
            _profielImageStore = profielImageStore;
        }

        [HttpGet("{id}", Name = "GetProfile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfileDTO>> GetProfile(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            AuthorizationResult authorization = await authorizationService.AuthorizeAsync(User, user, "ManageAccount");
            if (!authorization.Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            clsPerson person = user.PersonInfo;

            return Ok(new ProfileDTO
            {
                UserId = user.UserID,
                PersonId = person.PersonID,
                Email = user.Email,
                Role = user.Role,
                Firstname = person.Firstname,
                Midname = person.Midname,
                Thirdname = person.Thirdname,
                Lastname = person.Lastname,
                NationalNo = person.NationalNo,
                Phone = person.Phone,
                Gender = person.Gender,
                DateOfBirth = person.DateOfBirth,
                ProfileImage = person.ProfileImage,
            });
        }

        [HttpPut("{id}", Name = "UpdateProfile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateProfile(
            [Range(1, int.MaxValue)] int id,
            UpdatePersonDTO request,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);

            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            AuthorizationResult authorization = await authorizationService.AuthorizeAsync(User, user, "ManageAccount");
            if (!authorization.Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            clsPerson person = user.PersonInfo;

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

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The profile could not be saved.");
        }

        [HttpPost("{id}/image", Name = "UploadProfileImage")]
        [RequestSizeLimit(ProfileImageStore.MaxBytes)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<string>> UploadProfileImage(
            [Range(1, int.MaxValue)] int id,
            IFormFile image,
            [FromServices] IAuthorizationService authorizationService,
            CancellationToken cancellationToken
        )
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user != null)
            {
                AuthorizationResult authorization = await authorizationService.AuthorizeAsync(User, user, "ManageAccount");
                if (!authorization.Succeeded)
                {
                    return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");
                }
            } else
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");
            }

            string? newFileName = await _profielImageStore.SaveAsync(image, cancellationToken);
            if (newFileName == null)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "That file is not valid image file.");
            }

            clsPerson person = user.PersonInfo;
            string? oldFileName = person.ProfileImage;

            person.ProfileImage = newFileName;

            if (!await _personRepository.UpdateAsync(person))
            {
                _profielImageStore.Delete(newFileName);
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The picture could not be saved.");
            }

            _profielImageStore.Delete(oldFileName);

            return Ok(newFileName);
        }
    
        [HttpDelete("{id}/image", Name = "RemoveProfileImage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveProfileImage(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            AuthorizationResult authorization = await authorizationService.AuthorizeAsync(User, user, "ManageAccount");
            if (!authorization.Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found!");

            clsPerson person = user.PersonInfo;
            string? oldFileName = person.ProfileImage;

            if (oldFileName == null)
                return NoContent();

            person.ProfileImage = null;

            if (!await _personRepository.UpdateAsync(person))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The picture could not be removed.");

            _profielImageStore.Delete(oldFileName);

            return NoContent();
        }
    }
}
