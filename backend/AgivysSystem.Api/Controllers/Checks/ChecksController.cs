using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.Models.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;

namespace AgiVysSystem.Api.Controllers.Checks;

/// <summary>
/// Controlador responsável por verificações do sistema (Email, CPF, etc).
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ChecksController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;

    public ChecksController(UserManager<User> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    /// <summary>
    /// Verificação de Disponibilidade de E-mail
    /// </summary>
    /// <remarks>
    /// Consulta na base de dados (Identity) se o e-mail fornecido já está vinculado a alguma conta de usuário existente.
    /// Este endpoint é público e frequentemente utilizado durante etapas de pré-cadastro (Onboarding) para validação assíncrona de formulários em tempo real.
    /// </remarks>
    /// <param name="email">O endereço de e-mail completo a ser verificado.</param>
    /// <response code="200">Requisição bem-sucedida. Retorna um objeto indicando se o e-mail já existe (`true` ou `false`).</response>
    /// <response code="400">Formato de e-mail inválido ou parâmetro ausente.</response>
    /// <response code="500">Erro interno do servidor ao tentar consultar o banco de dados.</response>
    [AllowAnonymous]
    [HttpGet("check-email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            return BadRequest(new { message = "Formato de e-mail inválido." });
        }

        // Normaliza o e-mail para bater com o padrão do Identity (Upper case)
        var normalizedEmail = email.Trim().ToUpper();

        // Verifica na tabela de Usuários (Identity)
        var exists = await _userManager.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);

        return Ok(new { exists });
    }

    /// <summary>
    /// Verificação de Disponibilidade de CPF
    /// </summary>
    /// <remarks>
    /// Verifica se o documento (CPF) informado já possui um cadastro atrelado na tabela de Pessoas (Person).
    /// Endpoint público de utilidade para formulários. Suporta o recebimento do documento com ou sem máscara de formatação (pontos e traços), uma vez que a API higieniza a string mantendo apenas números.
    /// </remarks>
    /// <param name="document">O número do CPF a ser consultado.</param>
    /// <response code="200">Requisição bem-sucedida. Retorna um objeto indicando se o documento já existe (`true` ou `false`).</response>
    /// <response code="400">CPF com tamanho inválido ou string malformada.</response>
    /// <response code="500">Erro interno do servidor ao tentar consultar o banco de dados.</response>
    [AllowAnonymous]
    [HttpGet("check-cpf/{document}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckCpf(string document)
    {
        // Remove pontos e traços caso o front-end envie formatado
        var cleanDocument = new string(document.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(cleanDocument) || cleanDocument.Length != 11)
        {
            return BadRequest(new { message = "CPF inválido." });
        }

        // Verifica na tabela People se já existe o documento
        var exists = await _context.People.AnyAsync(p => p.Document == cleanDocument);

        return Ok(new { exists });
    }
}
