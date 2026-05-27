namespace AgiVysSystem.Api.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public AuthUserDto User { get; set; } = new AuthUserDto();
    public AuthPersonDto Person { get; set; } = new AuthPersonDto();
}

public class AuthUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? IdSystem { get; set; }
    public List<AuthUserRoleDto> Roles { get; set; } = new();
}

public class AuthUserRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class AuthPersonDto
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}
