using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;
using System.Security.Claims;

namespace ReportSchedulerApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportScheduleController : ControllerBase
    {
        private readonly IReportScheduleRepository _repo;

        public ReportScheduleController(IReportScheduleRepository repo)
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
            // For CreatedBy / UpdatedBy / DeletedBy
            var userActionBy = GetLoggedInUserId();

            model.E_By = userActionBy;
            var result = await _repo.SaveAsync(model);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // For display/logging only
            var personName = GetLoggedInPersonName();

            var result = await _repo.DeleteAsync(id, personName);
            return Ok(result);
        }

        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var actionBy = GetLoggedInUserId();
            var result = await _repo.SetActiveAsync(id, true, actionBy);
            return Ok(result);
        }

        [HttpPost("{id:int}/pause")]
        public async Task<IActionResult> Pause(int id)
        {
            var actionBy = GetLoggedInUserId();
            var result = await _repo.SetActiveAsync(id, false, actionBy);
            return Ok(result);
        }

        private string? GetLoggedInPersonName()
        {
            return User.FindFirst("personName")?.Value;
        }

        private int? GetLoggedInUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            return null;
        }
    }
}

