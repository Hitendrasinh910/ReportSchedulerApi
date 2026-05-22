using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReportSchedulerApi.Repositories.Interfaces;

namespace ReportSchedulerApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SchedulerLookupController : ControllerBase
    {
        private readonly ISchedulerLookupRepository _repo;

        public SchedulerLookupController(ISchedulerLookupRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] string? userType,
            [FromQuery] string? adminType)
        {
            var data = await _repo.GetUsersAsync(search, userType, adminType);
            return Ok(data);
        }

        [HttpGet("party-accounts")]
        public async Task<IActionResult> GetPartyAccounts(
            [FromQuery] string? search,
            [FromQuery] string? partyType,
            [FromQuery] string? branchType,
            [FromQuery] string? dealerType)
        {
            var data = await _repo.GetPartyAccountsAsync(search, partyType, branchType, dealerType);
            return Ok(data);
        }
    }
}
