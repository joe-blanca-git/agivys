using System.ComponentModel.DataAnnotations;

namespace AgiVysSystem.Api.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;

    // Opcional: quando ausente, autentica conta de plataforma (PrimaryAppSystemId == null),
    // igual a hoje. Usuários finais de um AppSystem específico precisam informar aqui a
    // qual sistema pertence sua conta, já que o mesmo e-mail pode existir em vários.
    public int? IdSystem { get; set; }
}