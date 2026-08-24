namespace AgiVysSystem.Api.DTOs;

/// <summary>
/// Mesmo shape do <see cref="LoginResponseDto"/> da v1, sem o token — na v2 ele
/// vai só no cookie HttpOnly <c>agivys_at</c>, nunca no corpo da resposta.
/// </summary>
public class LoginResponseV2Dto
{
    public DateTime Expiration { get; set; }
    public AuthUserDto User { get; set; } = new AuthUserDto();
    public AuthPersonDto Person { get; set; } = new AuthPersonDto();
    public List<int> SystemIds { get; set; } = new();
    public List<string> SystemNames { get; set; } = new();
}
