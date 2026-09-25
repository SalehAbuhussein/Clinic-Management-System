using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.WorkingDays;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Controllers
{
    [Route("api/WorkingDays")]
    [ApiController]
    public class WorkingDaysController : ControllerBase
    {
        private readonly IWorkingDayRepository _workingDayRepository;

        public WorkingDaysController(IWorkingDayRepository workingDayRepository)
        {
            _workingDayRepository = workingDayRepository;
        }

        [HttpGet(Name = "GetWorkingDays")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<WorkingDayDTO>>> GetWorkingDays(
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 10)] int perPage = 10
        )
        {
            PagedResult<clsWorkingDay.WorkingDayItem> workingDaysPage = await _workingDayRepository.FindAllAsync(page, perPage);

            List<WorkingDayDTO> workingDaysList = workingDaysPage.Items
                .Select(workingDay => new WorkingDayDTO
                {
                    Id = workingDay.Id,
                    WorkingDay = workingDay.WorkingDay,
                    StartTime = workingDay.StartTime,
                    EndTime = workingDay.EndTime,
                    IsWorkingDay = workingDay.IsWorkingDay
                }).ToList();

            return Ok(new PagedResult<WorkingDayDTO>(workingDaysList, workingDaysPage.Page, workingDaysPage.PerPage, workingDaysPage.Total));
        }
    }
}
