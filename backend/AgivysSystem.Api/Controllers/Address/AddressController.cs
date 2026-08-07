using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs;
using AgiVysSystem.Api.Models.People;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AgiVysSystem.Api.Controllers.Address;

/// <summary>
/// Controlador responsável pela gestão de endereços.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AddressController : ControllerBase
{
    private readonly AppDbContext _context;

    public AddressController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Listagem de Endereços do Usuário
    /// </summary>
    /// <remarks>
    /// Recupera todos os endereços físicos vinculados ao perfil pessoal (Person) do usuário atualmente autenticado.
    /// É necessário enviar o Bearer Token no cabeçalho da requisição.
    /// </remarks>
    /// <response code="200">Lista de endereços recuperada com sucesso.</response>
    /// <response code="401">Usuário não autenticado ou token inválido.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize]
    [HttpGet("my-address")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var addresses = await _context.AddressPeople
            .Where(a => a.Person.UserId == userId)
            .Select(a => new {
                a.Id,
                a.Description,
                a.ZipCode,
                a.Street,
                a.Number,
                a.Complement,
                a.Neighborhood,
                a.City,
                a.State
            })
            .ToListAsync();

        return Ok(addresses);
    }

    /// <summary>
    /// Cadastro de Novo Endereço
    /// </summary>
    /// <remarks>
    /// Adiciona um novo endereço físico ao perfil do usuário autenticado. 
    /// O usuário deve possuir um registro de `Person` previamente criado no banco de dados.
    /// </remarks>
    /// <param name="dto">Dados completos do endereço, incluindo CEP, Logradouro, Número, Bairro, Cidade e Estado.</param>
    /// <response code="201">Endereço cadastrado com sucesso.</response>
    /// <response code="400">Dados inválidos ou erro de validação.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="404">Perfil (Person) não localizado para o usuário autenticado.</response>
    /// <response code="500">Erro interno ao processar a inclusão.</response>
    [Authorize]
    [HttpPost("my-address")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddAddress([FromBody] AddressPersonDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
        
        if (person == null) 
            return NotFound(new { message = "Perfil pessoal não localizado." });

        var address = new AddressPerson
        {
            PersonId = person.Id,
            Description = dto.Description,
            ZipCode = dto.ZipCode,
            Street = dto.Street,
            Number = dto.Number,
            Complement = dto.Complement,
            Neighborhood = dto.Neighborhood,
            City = dto.City,
            State = dto.State
        };

        try 
        {
            _context.AddressPeople.Add(address);
            await _context.SaveChangesAsync();
            return StatusCode(201, new { message = "Endereço cadastrado com sucesso!", id = address.Id });
        }
        catch (Exception)
        {
            return BadRequest(new { message = "Erro ao processar o cadastro do endereço." });
        }
    }

    /// <summary>
    /// Atualização de Endereço Existente
    /// </summary>
    /// <remarks>
    /// Modifica os dados de um endereço previamente cadastrado.
    /// **Regra de Segurança**: O endereço a ser alterado deve pertencer estritamente ao perfil do usuário autenticado. Tentativas de alterar endereços de terceiros resultarão em bloqueio de acesso (HTTP 403).
    /// </remarks>
    /// <param name="id">ID único do endereço a ser atualizado.</param>
    /// <param name="dto">Objeto contendo os novos dados do endereço.</param>
    /// <response code="200">Endereço atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos no corpo da requisição.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. O endereço pertence a outro usuário.</response>
    /// <response code="404">Endereço não localizado na base de dados.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize]
    [HttpPut("my-address/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMyAddress(int id, [FromBody] AddressPersonDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var address = await _context.AddressPeople
            .Include(a => a.Person)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (address == null) 
            return NotFound(new { message = "Endereço não encontrado." });

        if (address.Person.UserId != userId)
            return StatusCode(403, new { message = "Você não tem permissão para alterar este endereço." });

        address.Description = dto.Description;
        address.ZipCode = dto.ZipCode;
        address.Street = dto.Street;
        address.Number = dto.Number;
        address.Complement = dto.Complement;
        address.Neighborhood = dto.Neighborhood;
        address.City = dto.City;
        address.State = dto.State;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Endereço atualizado com sucesso!" });
    }

    /// <summary>
    /// Exclusão de Endereço
    /// </summary>
    /// <remarks>
    /// Remove de forma permanente um endereço vinculado ao perfil do usuário autenticado.
    /// **Regra de Segurança**: A exclusão só será permitida caso o endereço pertença ao usuário logado, protegendo os dados contra acesso indevido (Insecure Direct Object Reference).
    /// </remarks>
    /// <param name="id">ID único numérico do endereço a ser removido.</param>
    /// <response code="200">Endereço removido permanentemente com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Ação negada. O endereço não pertence ao usuário.</response>
    /// <response code="404">O endereço especificado não foi encontrado.</response>
    /// <response code="500">Erro interno do servidor ao processar a exclusão.</response>
    [Authorize]
    [HttpDelete("my-address/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMyAddress(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var address = await _context.AddressPeople
            .Include(a => a.Person)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (address == null) 
            return NotFound(new { message = "Endereço não encontrado." });

        if (address.Person.UserId != userId)
            return StatusCode(403, new { message = "Ação não permitida." });

        _context.AddressPeople.Remove(address);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Endereço removido com sucesso." });
    }
}
