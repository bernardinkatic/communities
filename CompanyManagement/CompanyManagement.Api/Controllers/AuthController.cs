using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CompanyManagement.Api.Data;
using CompanyManagement.Api.Models;

namespace CompanyManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CompanyDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(CompanyDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Logins
                .FirstOrDefaultAsync(u => u.LoginUserName == request.Username && u.LoginPassword == request.Password);

            if (user == null)
            {
                return Unauthorized("Invalid credentials");
            }

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token, User = new { user.LoginNo, user.LoginUserName } });
        }

        private string GenerateJwtToken(Login user)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "your-super-secret-key-that-is-at-least-32-characters-long");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.LoginNo.ToString()),
                    new Claim(ClaimTypes.Name, user.LoginUserName)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}