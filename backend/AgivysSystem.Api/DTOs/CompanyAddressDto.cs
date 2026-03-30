namespace AgiVysSystem.Api.Dtos;

public record CompanyAddressDto(
    string Description,
    string ZipCode,
    string Street,
    string Number,
    string Complement,
    string Neighborhood,
    string City,
    string State
);