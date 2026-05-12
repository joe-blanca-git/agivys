namespace AgiVysSystem.Api.DTOs;

public record RegisterDto(
    // Dados do Usuário/Pessoa
    string Name,
    string Document,
    string Email,
    string Password,
    DateTime BirthDate,

    // Dados do Endereço Inicial
    string AddressDescription,
    string ZipCode,
    string Street,
    string Number,
    string Complement,
    string Neighborhood,
    string City,
    string State,

    // Relacionamento com Sistema
    int? SystemId = null
);