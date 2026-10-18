using Clinic_Business.Common;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Clinic_Business.Entities.clsAuditEntry;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Audit")]
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly IAuditRepository _auditRepository;

        public AuditController(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetAuditEntries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<AuditEntryDTO>>> GetAuditEntries(
            DateTimeOffset? from,
            DateTimeOffset? to,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 10)] int perPage = 10)
        {
            PagedResult<AuditEntryItem> entriesPage = await _auditRepository.FindAllAsync(from, to, page, perPage);

            List<AuditEntryDTO> itemsDTO = entriesPage.Items.Select(item => new AuditEntryDTO
            {
                Id = item.Id,
                OccurredAt = item.OccurredAt,
                ActorId = item.ActorUserID,
                ActorName = item.ActorName,
                Action = item.Action,
                TargetType = item.TargetType,
                TargetId = item.TargetID,
                Detail = item.Detail,
            }).ToList();

            return Ok(new PagedResult<AuditEntryDTO>(itemsDTO, page, perPage, entriesPage.Total));
        }
    }
}
