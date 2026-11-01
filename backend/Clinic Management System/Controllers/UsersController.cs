using Clinic_Business.Common;
using Clinic_DataAccess.Common.Models;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
        public async Task<ActionResult<PagedResult<UserDTO>>> GetUsers(string? fullname, string? email, bool? active, [Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            PagedResult<clsUser.UserItem> usersPage = await _userRepository.FindAllAsync(fullname, email, active, page, perPage);

            List<UserDTO> usersList = usersPage.Items
                .Select(user => new UserDTO
                {
                    Id = user.Id,
                    Fullname = user.Fullname,
                    Email = user.Email,
                    Active = user.Active,
                    Role = user.Role,
                }).ToList();

            return Ok(new PagedResult<UserDTO>(usersList, usersPage.Page, usersPage.PerPage, usersPage.Total));
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
        public async Task<ActionResult> UpdateUser(
            [Range(1, int.MaxValue)] int id,
            UpdateUserDTO updatedUserDTO,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);

            if (user == null || !(await authorizationService.AuthorizeAsync(User, user, "ManageAccount")).Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found");

            clsUser? existingEmailUser = await _userRepository.FindByEmailAsync(updatedUserDTO.Email);
            bool isEmailTakenByAnotherUser = existingEmailUser != null && existingEmailUser.UserID != id;
            if (isEmailTakenByAnotherUser)
                return Problem(statusCode: StatusCodes.Status409Conflict, detail: "That email is already in use by another user.");

            string previousRole = user.Role;
            bool previousActive = user.Active;

            if (previousRole == "Doctor" && updatedUserDTO.Role is not null && updatedUserDTO.Role != previousRole)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "Delete them from the Doctors screen before changing their role.");

            user.Email = updatedUserDTO.Email;
            user.Role = updatedUserDTO.Role ?? user.Role;
            user.Active = updatedUserDTO.Active ?? user.Active;

            int? adminId = CurrentUserId();

            if (await user.UpdateAsync(_AuditRowsFor(user, adminId, previousRole, previousActive)))
            {
                _logger.LogInformation(
                    "Admin {ActorId} updated user {UserId}: role {PreviousRole} -> {Role}, active {PreviousActive} -> {Active}",
                    adminId, id, previousRole, user.Role, previousActive, user.Active);

                return NoContent();
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The user could not be saved.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeleteUser(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            clsUser? user = await _userRepository.FindByIdAsync(id);

            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found");

            if (!(await authorizationService.AuthorizeAsync(User, user, "DeleteAccount")).Succeeded)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "Only a doctor or staff account can be deleted, and not your own. Deactivate it instead.");

            try
            {
                if (await _userRepository.DeleteAsync(id))
                {
                    _logger.LogInformation("Admin {ActorId} deleted user {UserId}", adminId, id);

                    return NoContent();
                }
            }
            catch (SqlException exception) when (exception.Number == 547)
            {
                // Audit entries, a doctor record or leave requests still point at
                // this account, and the clinic keeps that history.
                _logger.LogWarning(exception, "Admin {ActorId} could not delete user {UserId}: rows still reference it", adminId, id);

                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "This account has history that cannot be removed. Deactivate it instead.");
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The user could not be deleted.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/deactivate", Name = "DeactivateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeactivateUser(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            clsUser? user = await _userRepository.FindByIdAsync(id);
            var authorization = await authorizationService.AuthorizeAsync(User, user, "ManageAccount");
            if (user == null || !authorization.Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found");

            if (userId == id)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "You cannot deactivate your own account.");

            if (await user.DeactivateAsync(userId))
            {
                _logger.LogInformation("Admin {ActorId} deactivated user {UserId}", userId, id);

                return NoContent();
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The user could not be deactivated.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/activate", Name = "ActivateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> Activate(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService
        )
        {
            clsUser? user = await _userRepository.FindByIdAsync(id);
            if (user == null || !(await authorizationService.AuthorizeAsync(User, user, "ManageAccount")).Succeeded)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"User with id {id} not found");

            int? adminId = CurrentUserId();

            if (await user.ActivateAsync(adminId))
            {
                _logger.LogInformation("Admin {ActorId} activated user {UserId}", adminId, id);

                return NoContent();
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The user could not be activated.");
        }

        /// <summary>
        /// What an update should record, which is only what actually changed.
        /// </summary>
        /// <param name="user">The account after the edit.</param>
        /// <param name="adminId">Who made the edit.</param>
        /// <param name="previousRole">The role before it.</param>
        /// <param name="previousActive">Whether the account was enabled before it.</param>
        /// <returns>An entry per change, empty when the edit changed neither.</returns>
        private static List<AuditRow> _AuditRowsFor(clsUser user, int? adminId, string previousRole, bool previousActive)
        {
            List<AuditRow> auditRows = new List<AuditRow>();

            if (user.Role != previousRole)
                auditRows.Add(new AuditRow(adminId, AuditAction.UserRoleChanged, "User", user.UserID, $"{previousRole} -> {user.Role}"));

            if (user.Active != previousActive)
                auditRows.Add(new AuditRow(adminId, user.Active ? AuditAction.UserActivated : AuditAction.UserDeactivated, "User", user.UserID));

            return auditRows;
        }

        int? CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;
    }
}
