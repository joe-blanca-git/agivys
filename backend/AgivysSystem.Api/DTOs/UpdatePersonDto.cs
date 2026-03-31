namespace AgiVysSystem.Api.DTOs;

public record UpdatePersonDto(
    string Name,
    DateTime BirthDate,
    string Email
);