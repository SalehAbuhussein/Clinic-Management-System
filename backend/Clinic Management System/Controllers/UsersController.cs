using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetUsers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<List<UserDTO>> GetUsers([Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            perPage = Math.Clamp(perPage, 1, 10);

            DataTable table = clsUser.FindUsers(page, perPage);
            List<UserDTO> usersList = new List<UserDTO>();

            foreach (DataRow row in table.Rows)
            {
                usersList.Add(new UserDTO
                {
                    Id = row.Field<int>("UserID"),
                    Fullname = row.Field<string>("Fullname") ?? string.Empty,
                    Email = row.Field<string>("Email") ?? string.Empty,
                    Active = row.Field<bool>("Active"),
                    Role = row.Field<string>("Role") ?? string.Empty,
                });
            }

            return Ok(usersList);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<UserDTO> GetUser([Range(1, int.MaxValue)] int id)
        {
            clsUser? user = clsUser.FindUser(id);
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
        public ActionResult UpdateUser([Range(1, int.MaxValue)] int id, UpdateUserDTO updatedUserDTO)
        {
            clsUser? user = clsUser.FindUser(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"User with id {id} not found");

            clsUser? existingEmailUser = clsUser.FindByEmail(updatedUserDTO.Email);
            bool isEmailTakenByAnotherUser = existingEmailUser != null && existingEmailUser.UserID != id;
            if (isEmailTakenByAnotherUser)
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    detail: "That email is already in use by another user.");

            user.Email = updatedUserDTO.Email; // TODO Check if the email is assigned to another user to return error
            user.Role = updatedUserDTO.Role ?? user.Role; // TODO: Check who really can change the role
            user.Active = updatedUserDTO.Active ?? user.Active;

            if (user.Update())
                return NoContent();

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
        public ActionResult DeactivateUser([Range(1, int.MaxValue)] int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (userId == id)
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "You cannot deactivate your own account.");

            clsUser? user = clsUser.FindUser(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"User with id {id} not found");

            if (user.Deactivate())
                return NoContent();

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
        public ActionResult Activate([Range(1, int.MaxValue)] int id)
        {
            clsUser? user = clsUser.FindUser(id);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"User with id {id} not found");

            if (user.Activate())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The user could not be activated.");
        }
    }
}
