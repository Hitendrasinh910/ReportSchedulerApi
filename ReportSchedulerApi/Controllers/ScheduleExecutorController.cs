using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReportSchedulerApi.Repositories.Interfaces;

namespace ReportSchedulerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleExecutorController : ControllerBase
    {
        private readonly IScheduleExecutorService _executor;

        public ScheduleExecutorController(IScheduleExecutorService executor)
        {
            _executor = executor;
        }

        [HttpPost("run/{idSchedule:int}")]
        public async Task<IActionResult> RunSchedule(int idSchedule)
        {
            await _executor.ExecuteScheduleAsync(idSchedule);

            return Ok(new
            {
                message = "Schedule executed successfully."
            });
        }

        [HttpPost("run-active")]
        public async Task<IActionResult> RunActiveSchedules()
        {
            await _executor.ExecuteDueSchedulesAsync();

            return Ok(new
            {
                message = "Due active schedules executed successfully."
            });
        }
    }
}

