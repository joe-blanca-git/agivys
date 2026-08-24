using AgiVysSystem.Api.Models.User;

namespace AgiVysSystem.Api.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user, IEnumerable<string> roles, string? personName);
}
