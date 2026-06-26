using System.ComponentModel.DataAnnotations;

namespace AgiVysSystem.Api.DTOs;

public record RegisterSystemUserDto
{
    [Required(ErrorMessage = "Campo idSystem é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "idSystem deve ser um inteiro maior que zero.")]
    public int IdSystem { get; init; }

    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Data de nascimento é obrigatória.")]
    public DateTime BirthDate { get; init; }
}
