using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;

namespace ReportSchedulerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportScheduleController : ControllerBase
    {
        private readonly IReportScheduleRepo _repo;

        public ReportScheduleController(IReportScheduleRepo repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string search = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _repo.GetAllAsync(search, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _repo.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] ReportScheduleDto model)
        {
            var result = await _repo.SaveAsync(model);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            [FromQuery] string deletedBy)
        {
            var result = await _repo.DeleteAsync(id, deletedBy);
            return Ok(result);
        }

        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(
            int id,
            [FromQuery] int? userActionBy)
        {
            var result = await _repo.SetActiveAsync(id, true, userActionBy);
            return Ok(result);
        }

        [HttpPost("{id:int}/pause")]
        public async Task<IActionResult> Pause(
            int id,
            [FromQuery] int? userActionBy)
        {
            var result = await _repo.SetActiveAsync(id, false, userActionBy);
            return Ok(result);
        }
    }
}

