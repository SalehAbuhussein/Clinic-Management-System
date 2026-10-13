using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using static Clinic_Business.Entities.clsLeave;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Leaves")]
    [ApiController]
    public class LeavesController : ControllerBase
    {
        private readonly ILeaveRepository _leaveRepository;
        private readonly IUserRepository _userRepository;

        public LeavesController(ILeaveRepository leaveRepository, IUserRepository userRepository)
        {
            _leaveRepository = leaveRepository;
            _userRepository = userRepository;
        }

        [Authorize]
        [HttpGet(Name = "GetLeaves")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<LeaveDTO>>> GetLeaves(
            DateTimeOffset? from,
            DateTimeOffset? to,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 10)] int perPage = 10)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            string role = User.FindFirstValue(ClaimTypes.Role)!;
            int? scopedUserId = role == "Admin" ? null : userId;

            PagedResult<LeaveItem> leavesPage = await _leaveRepository.FindAllLeavesAsync(scopedUserId, from, to, page, perPage);
            List<LeaveDTO> itemsDTO = leavesPage.Items.Select(item => new LeaveDTO
            {
                Id = item.Id,
                Fullname = item.Fullname,
                From = item.StartAt,
                To = item.EndAt,
                Reason = item.Reason,
                Status = item.Status,
            }).ToList();

            return Ok(new PagedResult<LeaveDTO>(itemsDTO, page, perPage, leavesPage.Total));
        }

        [Authorize]
        [HttpGet("{id}", Name = "GetLeaveByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LeaveDTO?>> GetLeaveByID(
            [Range(1, int.MaxValue)] int id,
            [FromServices] IAuthorizationService authorizationService)
        {
            clsLeave? leave = await _leaveRepository.FindAsync(id);
            if (leave != null)
            {
                AuthorizationResult authorization = await authorizationService.AuthorizeAsync(User, leave, "ViewLeave");
                if (!authorization.Succeeded)
                    return Forbid();
            }

            if (leave == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Leave with id {id} not found!");

            return Ok(new LeaveDTO
            {
                Id = id,
                From = leave.StartAt,
                To = leave.EndAt,
                Reason = leave.Reason,
                Status = leave.Status,
                Fullname = leave.UserInfo.PersonInfo.Fullname
            });
        }

        [Authorize]
        [HttpPost(Name = "AddLeave")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<LeaveDTO>> AddLeave(CreateLeaveDTO request)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            clsUser? user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: "Your account no longer exists");

            if (user.Role == "Admin")
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "Only doctor and staff can request a leave!");

            if (request.To <= request.From)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "To must be greater than from.");

            if (await _leaveRepository.HasUserLeaveAsync(userId, request.From, request.To))
                return Problem(statusCode: StatusCodes.Status409Conflict, detail: "User has already existing leave");

            clsLeave leave = new clsLeave();
            leave.UserID = userId;
            leave.StartAt = request.From;
            leave.EndAt = request.To;
            leave.Reason = request.Reason;

            if (await leave.SaveAsync())
            {
                return CreatedAtRoute("GetLeaveByID", new { id = leave.LeaveID }, new LeaveDTO
                {
                    Id = leave.LeaveID,
                    From = leave.StartAt,
                    To = leave.EndAt,
                    Reason= leave.Reason,
                    Status = leave.Status,
                    Fullname = user.PersonInfo.Fullname
                });
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The Leave could not be created!");
        }

        [Authorize]
        [HttpPut("{id}", Name = "UpdateLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateLeave(
            [Range(1, int.MaxValue)] int id,
            UpdateLeaveDTO request,
            [FromServices] IAuthorizationService authorizationService)
        {
            clsLeave? leave = await _leaveRepository.FindAsync(id);
            if (leave != null)
            {
                AuthorizationResult authorization = await authorizationService.AuthorizeAsync(User, leave, "UpdateLeave");
                if (!authorization.Succeeded)
                    return Forbid();
            }

            if (leave == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Leave with id {id} not found!");

            leave.Reason = request.Reason;

            if (await leave.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The Leave could not be updated!");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("approve/{id}", Name = "ApproveLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ApproveLeave([Range(1, int.MaxValue)] int id)
        {
            clsLeave? leave = await _leaveRepository.FindAsync(id);
            if (leave == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Leave with id {id} not found!");

            leave.Status = (int)clsLeave.enStatus.Approved;

            if (await leave.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The Leave could not be saved.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("reject/{id}", Name = "RejectLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RejectLeave([Range(1, int.MaxValue)] int id)
        {
            clsLeave? leave = await _leaveRepository.FindAsync(id);
            if (leave == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Leave with id {id} not found!");

            leave.Status = (int)clsLeave.enStatus.Rejected;

            if (await leave.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The Leave could not be saved.");
        }
    }
}