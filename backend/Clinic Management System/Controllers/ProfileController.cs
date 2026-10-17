using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.People;
using Clinic_Management_System.DTOs.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IPersonRepository _personRepository;
        private readonly IUserRepository _userRepository;

        public ProfileController(IPersonRepository personRepository, IUserRepository userRepository)
        {
            _personRepository = personRepository;
            _userRepository = userRepository;
        }

        [HttpGet(Name = "GetProfile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfileDTO>> GetProfile()
        {
            clsUser? user = await _userRepository.FindByIdAsync(CallerId());

            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: "Your account no longer exists");

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
            person.ProfileImage = request.ProfileImage;

            if (!person.IsValid())
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "Check the name, phone and date of birth.");

            if (await _personRepository.UpdateAsync(person))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The profile could not be saved.");
        }

        private int CallerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
