using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReportSchedulerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;

        public AuthController(
            IUserRepository userRepository,
            IConfiguration configuration)
        {
            _userRepo = userRepository;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            var user = await _userRepo.ValidateLoginAsync(
                request.Username,
                request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var token = GenerateJwtToken(user, out DateTime expiresAt);

            return Ok(new LoginResponseDto
            {
                Token = token,
                IDUser = user.IDUser,
                PersonName = user.PersonName,
                UserType = user.UserType,
                Username = user.Username,
                ExpiresAt = expiresAt
            });
        }

        private string GenerateJwtToken(UserDto user, out DateTime expiresAt)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var expireMinutes = Convert.ToInt32(
                _configuration["Jwt:ExpireMinutes"] ?? "1440");

            expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IDUser.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim("personName", user.PersonName ?? ""),
                new Claim("userType", user.UserType ?? "")
            };

            if (!string.IsNullOrWhiteSpace(user.UserType))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.UserType));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
