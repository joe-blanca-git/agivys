namespace AgiVysSystem.Api.Dtos;

public class CreateCompanyDto
{
    public string Name { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int UserOwnerId { get; set; }
}

public class UpdateCompanyDto
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}