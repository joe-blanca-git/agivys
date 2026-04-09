namespace AgiVysSystem.Api.Dtos;

public class CreateCompanyWithAddressDto
{
    // Dados da Empresa
    public string Name { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int UserOwnerId { get; set; }

    // Dados do Endereço
    public string Description { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}