using Clinic_Business.Common;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Audit;
using Clinic_Management_System.Export;
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

        [Authorize(Roles = "Admin")]
        [HttpGet("export", Name = "ExportAuditEntries")]
        [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, ExcelFile.SpreadsheetContentType)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ExportAuditEntries(DateTimeOffset? from, DateTimeOffset? to)
        {
            if (await _auditRepository.GetTotalCountAsync(from, to) > ExcelFile.MaxRows)
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"That range holds more than {ExcelFile.MaxRows:N0} entries. Narrow it and try again.");

            PagedResult<AuditEntryItem> entriesPage = await _auditRepository.FindAllAsync(from, to, 1, ExcelFile.MaxRows);

            return ExcelFile.Build("Audit", ExportColumns, entriesPage.Items);
        }

        /// <summary>
        /// Action and TargetType stay as the API stores them. The screen softens them
        /// into sentences; an export is read by someone filtering and counting, who
        /// wants the stored key.
        /// </summary>
        private static readonly ExcelColumn<AuditEntryItem>[] ExportColumns =
        [
            new("When", e => e.OccurredAt),
            new("Actor", e => e.ActorName),
            new("Actor ID", e => e.ActorUserID),
            new("Action", e => e.Action),
            new("Target type", e => e.TargetType),
            new("Target ID", e => e.TargetID),
            new("Detail", e => e.Detail),
        ];
    }
}
