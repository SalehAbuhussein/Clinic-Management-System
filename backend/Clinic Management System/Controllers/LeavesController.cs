using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Clinic_Business.Entities.clsLeave;

namespace Clinic_Management_System.Controllers
{
    [Route("api/[controller]")]
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

        [Authorize(Roles = "Admin")]
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
            perPage = Math.Clamp(perPage, 1, 10);

            PagedResult<LeaveItem> leavesPage = await _leaveRepository.FindAllLeavesAsync(from, to, page, perPage);

            List<LeaveDTO> leavesDTO = leavesPage.Items.Select(leave => new LeaveDTO
            {
                Id = leave.Id,
                From = leave.StartAt,
                To = leave.EndAt,
                Reason = leave.Reason,
                IsApproved = leave.IsApproved
            }).ToList();

            return Ok(new PagedResult<LeaveDTO>(leavesDTO, leavesPage.Page, leavesPage.PerPage, leavesPage.Total));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetLeaveByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LeaveDTO?>> GetLeaveByID([Range(1, int.MaxValue)] int id)
        {
            clsLeave? leave = await _leaveRepository.FindAsync(id);

            if (leave == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Leave with id {id} not found!");

            return Ok(new LeaveDTO
            {
                Id = id,
                From = leave.StartAt,
                To = leave.EndAt,
                Reason = leave.Reason,
                IsApproved = leave.IsApproved,
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost(Name = "AddLeave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<LeaveDTO>> AddLeave(CreateLeaveDTO request)
        {
            if (await _userRepository.FindByIdAsync(request.UserId) == null)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Invalid User");

            clsLeave leave = new clsLeave();
            leave.UserID = request.UserId;
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
                    IsApproved = leave.IsApproved
                });
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The Leave could not be created!");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}", Name = "UpdateLeave")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateLeave([Range(1, int.MaxValue)] int id, UpdateLeaveDTO request)
        {
            clsLeave? leave = await _leaveRepository.FindAsync(id);

            if (leave == null)
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Leave with id {id} not found!");

            leave.Reason = request.Reason;
            leave.IsApproved = request.IsApproved;

            if (await leave.SaveAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The Leave could not be updated!");
        }
    }
}
