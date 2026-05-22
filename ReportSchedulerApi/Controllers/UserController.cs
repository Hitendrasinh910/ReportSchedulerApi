using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;
using System.Security.Claims;

namespace ReportSchedulerApi.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _repo;

        public UserController(IUserRepository repo)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _repo.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] UserDto model)
        {
            var actionBy = GetLoggedInUserId();
            model.E_By = actionBy;
            var result = await _repo.SaveAsync(model);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // For display/logging only
            var personName = GetLoggedInPersonName();

            var result = await _repo.DeleteAsync(id, personName);
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
