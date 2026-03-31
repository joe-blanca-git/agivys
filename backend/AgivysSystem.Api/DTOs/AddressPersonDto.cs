namespace AgiVysSystem.Api.DTOs;

public record AddressPersonDto(
    string Description,
    string ZipCode,
    string Street,
    string Number,
    string Complement,
    string Neighborhood,
    string City,
    string State
);