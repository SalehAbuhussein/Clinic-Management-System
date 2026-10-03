using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Stats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Stats")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly IStatsRepository _statsRepository;

        public StatsController(IStatsRepository statsRepository)
        {
            _statsRepository = statsRepository;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetStats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<StatsDTO>> GetStats(DateTimeOffset todayFrom, DateTimeOffset from, DateTimeOffset to)
        {
            if (from >= to)
                return Problem(statusCode: StatusCodes.Status400BadRequest,
                    detail: "The start of the range must be before its end.");

            // One bar per day, capped at a quarter so nobody asks for decades of them.
            int days = Math.Clamp((int)(to - from).TotalDays, 1, 92);

            clsStats.ClinicStats stats = await _statsRepository.FindAsync(todayFrom, from, days);

            return Ok(new StatsDTO
            {
                InClinic = stats.InClinic,
                AppointmentsToday = stats.AppointmentsToday,
                AttendedToday = stats.AttendedToday,
                NoShowToday = stats.NoShowToday,
                Patients = stats.Patients,
                AppointmentsByDay = stats.AppointmentsByDay,
            });
        }
    }
}
