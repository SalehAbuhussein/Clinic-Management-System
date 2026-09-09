using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserRepository userRepository,
            ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetUsers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<UserDTO>>> GetUsers([Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            List<clsUser.UserItem> users = await _userRepository.FindUsersAsync(page, perPage);

            List<UserDTO> usersList = users
                .Select(user => new UserDTO
                {
                    Id = user.Id,
                    Fullname = user.Fullname,
                    Email = user.Email,
                    Active = user.Active,
                    Role = user.Role,
                }).ToList();

            return Ok(usersList);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDTO>> GetUser([Range(1, int.MaxValue)] int id)
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"User with id {id} not found");

            UserDTO userDTO = new UserDTO
            {
                Id = user.UserID,
                Email = user.Email,
                Fullname = user.PersonInfo?.Fullname ?? "",
                Role = user.Role,
                Active = user.Active,
            };

            return Ok(userDTO);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}", Name = "UpdateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateUser([Range(1, int.MaxValue)] int id, UpdateUserDTO updatedUserDTO)
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"User with id {id} not found");
            
            clsUser? existingEmailUser = await _userRepository.FindByEmailAsync(updatedUserDTO.Email);
            bool isEmailTakenByAnotherUser = existingEmailUser != null && existingEmailUser.UserID != id;
            if (isEmailTakenByAnotherUser)
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "That email is already in use by another user.");

            string previousRole = user.Role;
            bool previousActive = user.Active;

            user.Email = updatedUserDTO.Email; // TODO Check if the email is assigned to another user to return error
            user.Role = updatedUserDTO.Role ?? user.Role; // TODO: Check who really can change the role
            user.Active = updatedUserDTO.Active ?? user.Active;

            if (await user.Update())
            {
                _logger.LogInformation(
                    "Admin {ActorId} updated user {UserId}: role {PreviousRole} -> {Role}, active {PreviousActive} -> {Active}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier), id,
                    previousRole, user.Role, previousActive, user.Active);

                return NoContent();
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The user could not be saved.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/deactivate", Name = "DeactivateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeactivateUser([Range(1, int.MaxValue)] int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (userId == id)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "You cannot deactivate your own account.");

            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"User with id {id} not found");

            if (await user.Deactivate())
            {
                _logger.LogInformation("Admin {ActorId} deactivated user {UserId}", userId, id);

                return NoContent();
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The user could not be deactivated.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/activate", Name = "ActivateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> Activate([Range(1, int.MaxValue)] int id)
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"User with id {id} not found");

            if (await user.Activate())
            {
                _logger.LogInformation("Admin {ActorId} activated user {UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier), id);

                return NoContent();
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The user could not be activated.");
        }
    }
}
