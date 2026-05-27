using System.ComponentModel.DataAnnotations;

namespace AgiVysSystem.Api.DTOs;

public record RegisterDto
{
    [Required(ErrorMessage = "Campo idSystem é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "idSystem deve ser um inteiro maior que zero.")]
    public int IdSystem { get; init; }

    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Documento é obrigatório.")]
    public string Document { get; init; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Data de nascimento é obrigatória.")]
    public DateTime BirthDate { get; init; }

    [Required(ErrorMessage = "Descrição do endereço é obrigatória.")]
    public string AddressDescription { get; init; } = string.Empty;

    [Required(ErrorMessage = "CEP é obrigatório.")]
    public string ZipCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "Logradouro é obrigatório.")]
    public string Street { get; init; } = string.Empty;

    [Required(ErrorMessage = "Número é obrigatório.")]
    public string Number { get; init; } = string.Empty;

    public string? Complement { get; init; }

    [Required(ErrorMessage = "Bairro é obrigatório.")]
    public string Neighborhood { get; init; } = string.Empty;

    [Required(ErrorMessage = "Cidade é obrigatória.")]
    public string City { get; init; } = string.Empty;

    [Required(ErrorMessage = "Estado é obrigatório.")]
    public string State { get; init; } = string.Empty;
}