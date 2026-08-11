using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs;
using AgiVysSystem.Api.Models.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AgiVysSystem.Api.Controllers.Person;

/// <summary>
/// Controlador responsável pela gestão de dados pessoais.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PersonController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;

    public PersonController(UserManager<User> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    /// <summary>
    /// Atualização Completa de Perfil (Person)
    /// </summary>
    /// <remarks>
    /// Substitui integralmente os dados pessoais do usuário autenticado.
    /// **Importante**: Caso o E-mail seja alterado, o login (UserName) na base do Identity também será atualizado simultaneamente para manter a consistência de acesso.
    /// </remarks>
    /// <param name="model">Objeto contendo todos os dados atualizados.</param>
    /// <response code="200">Perfil e credenciais de login atualizados com sucesso.</response>
    /// <response code="400">Dados inválidos ou erro durante a atualização no Identity.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="404">Registro de pessoa física (Person) não localizado para este usuário.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePerson([FromBody] UpdatePersonDto model)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
        
        var userId = int.Parse(userIdClaim);

        // 1. Busca a Pessoa
        var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
        if (person == null) return NotFound(new { message = "Dados não encontrados." });

        // 2. Busca o Usuário (Identity) para mudar o login
        var user = await _userManager.FindByIdAsync(userId.ToString());

        try 
        {
            // Atualiza na tabela People
            person.Name = model.Name;
            person.BirthDate = model.BirthDate;
            person.Email = model.Email;
            person.Phone = model.Phone;
            person.Document = string.IsNullOrWhiteSpace(model.Document) ? null : model.Document;

            // ATUALIZA O LOGIN (Identity)
            if (user != null)
            {
                user.Email = model.Email;
                user.UserName = model.Email; // Isso garante que o login mude!
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Dados e login atualizados com sucesso!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Erro ao atualizar.", details = ex.Message });
        }
    }
    /// <summary>
    /// Consulta de Perfil do Usuário Autenticado
    /// </summary>
    /// <remarks>
    /// Recupera os dados pessoais (Person) atrelados ao usuário logado, utilizando o ID presente no Bearer Token (ClaimTypes.NameIdentifier).
    /// Endpoint padrão para carregar a página "Meu Perfil" no front-end.
    /// </remarks>
    /// <response code="200">Dados do perfil recuperados com sucesso.</response>
    /// <response code="401">Usuário não autenticado (Token ausente ou inválido).</response>
    /// <response code="404">Perfil (Person) não localizado na base de dados.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPerson()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
        
        var userId = int.Parse(userIdClaim);

        var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
        if (person == null) return NotFound(new { message = "Dados não encontrados." });

        return Ok(person);
    }

    /// <summary>
    /// Consulta de Perfil por ID (Admin/Dev)
    /// </summary>
    /// <remarks>
    /// Recupera os dados de qualquer perfil (Person) através de seu ID.
    /// **Regra de Segurança**: Endpoint restrito apenas a administradores ou desenvolvedores da plataforma.
    /// </remarks>
    /// <param name="id">ID numérico do perfil (Person).</param>
    /// <response code="200">Dados do perfil recuperados com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. Apenas Admin e Dev têm acesso.</response>
    /// <response code="404">Perfil (Person) não localizado.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize(Roles = "Dev,Admin")]
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPersonById(int id)
    {
        var person = await _context.People.FirstOrDefaultAsync(p => p.Id == id);
        if (person == null) return NotFound(new { message = "Dados não encontrados." });

        return Ok(person);
    }

    /// <summary>
    /// Atualização Parcial de Perfil (PATCH)
    /// </summary>
    /// <remarks>
    /// Modifica parcialmente os dados do usuário autenticado.
    /// Apenas os atributos enviados no payload serão alterados; os demais permanecerão intactos.
    /// Se o E-mail for enviado no payload, o login (UserName) correspondente no Identity também será atualizado.
    /// </remarks>
    /// <param name="model">Objeto contendo os dados a serem atualizados (campos opcionais).</param>
    /// <response code="200">Dados parciais e credenciais (caso e-mail fornecido) atualizados com sucesso.</response>
    /// <response code="400">Payload malformado ou erro no processamento.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="404">Perfil (Person) não localizado para este usuário.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize]
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PatchPerson([FromBody] PatchPersonDto model)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
        
        var userId = int.Parse(userIdClaim);

        var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
        if (person == null) return NotFound(new { message = "Dados não encontrados." });

        var user = await _userManager.FindByIdAsync(userId.ToString());

        try 
        {
            if (model.Name != null) person.Name = model.Name;
            if (model.BirthDate.HasValue) person.BirthDate = model.BirthDate.Value;
            if (model.Phone != null) person.Phone = model.Phone;
            if (model.Document != null) person.Document = string.IsNullOrWhiteSpace(model.Document) ? null : model.Document;

            if (model.Email != null)
            {
                person.Email = model.Email;
                if (user != null)
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Dados atualizados com sucesso!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Erro ao atualizar.", details = ex.Message });
        }
    }
}
