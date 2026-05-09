using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;

namespace ReportSchedulerApi.Controllers
{
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
            var result = await _repo.SaveAsync(model);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Change this when you have logged-in user info
            var deletedBy = "1";

            var result = await _repo.DeleteAsync(id, deletedBy);
            return Ok(result);
        }
    }
}
