using Microsoft.AspNetCore.Mvc;
using SimpleWallet.Application.Interfaces;
using SimpleWallet.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SimpleWallet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthenticationController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Demo credentials must never authenticate users in production.
        // Real users and their wallet ownership will be implemented separately.
        if (!_environment.IsDevelopment())
            return NotFound();

        // kommer bytas mot DB lookup fÃ¶r userdata sen
        var users = new Dictionary<string, (string Password, string Role)>
        {
            { "admin", ("admin123", "Admin") },
            { "user", ("user123", "User") }
        };

        if (!users.TryGetValue(request.Username, out var userData) || userData.Password != request.Password)
            return Unauthorized();


        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.Username),
            new(ClaimTypes.Role, userData.Role)
        };

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var key = _configuration["Jwt:Key"];

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { token = tokenString });
    }
}
