using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgiVysSystem.Api.Interfaces;
using AgiVysSystem.Api.Models.User;
using Microsoft.IdentityModel.Tokens;

namespace AgiVysSystem.Api.Service;

/// <summary>
/// Geração do JWT de sessão. Extraído do AuthenticationController para ser
/// reaproveitado tanto pelo login v1 (token no corpo) quanto pelo v2 (token em cookie).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, IEnumerable<string> roles, string? personName)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        if (!string.IsNullOrWhiteSpace(personName))
        {
            claims.Add(new Claim("name", personName));
            claims.Add(new Claim(ClaimTypes.Name, personName));
        }

        foreach (var us in user.UserSystems)
        {
            claims.Add(new Claim("idSystem", us.AppSystemId.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(4);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
