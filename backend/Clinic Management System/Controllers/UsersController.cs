using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public ActionResult<List<UserDTO>> GetUsers(int page = 1, int perPage = 10)
        {
            if (page < 1 || perPage < 1)
                return BadRequest("Invalid parameters");

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
        public ActionResult<UserDTO> GetUser(int id)
        {
            if (id < 1)
                return BadRequest("Invalid user id");

            clsUser? user = clsUser.FindUser(id);
            if (user == null)
                return NotFound($"User with id {id} not found");

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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult UpdateUser(int id, UpdateUserDTO updatedUserDTO)
        {
            if (id < 1)
                return BadRequest("Invalid user id");

            clsUser? user = clsUser.FindUser(id);
            if (user == null)
                return NotFound($"User with id {id} not found");

            clsUser? existingEmailUser = clsUser.FindByEmail(updatedUserDTO.Email);
            bool isEmailTakenByAnotherUser = existingEmailUser != null && existingEmailUser.UserID != id;
            if (isEmailTakenByAnotherUser)
                return BadRequest("Email Already in use");

            user.Email = updatedUserDTO.Email; // TODO Check if the email is assigned to another user to return error
            user.Role = updatedUserDTO.Role ?? user.Role; // TODO: Check who really can change the role
            user.Active = updatedUserDTO.Active ?? user.Active;

            if (user.Save())
                return Ok($"User with id {id} updated successfully");

            return BadRequest("Invalid user data");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/deactivate", Name = "DeactivateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult DeactivateUser(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (id < 1)
                return BadRequest("Invalid user id");

            if (userId == id)
                return BadRequest("Invalid operation");

            clsUser? user = clsUser.FindUser(id);
            if (user == null)
                return NotFound($"User with id {id} not found");

            if (user.Deactivate())
                return Ok("User has been deactivated successfully");

            return BadRequest("Invalid user data");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/activate", Name = "ActivateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult Activate(int id)
        {
            if (id < 1)
                return BadRequest("Invalid user id");

            clsUser? user = clsUser.FindUser(id);
            if (user == null)
                return NotFound($"User with id {id} not found");

            if (user.Activate())
                return Ok("User has been activated successfully");

            return BadRequest("Invalid user data");
        }
    }
}
