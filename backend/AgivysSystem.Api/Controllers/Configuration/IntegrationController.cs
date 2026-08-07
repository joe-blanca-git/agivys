using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs;
using AgiVysSystem.Api.Models.Configuration;
using Asp.Versioning;

namespace AgiVysSystem.Api.Controllers.Configuration;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Dev")]
public class IntegrationController : ControllerBase
{
    private readonly AppDbContext _context;

    public IntegrationController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Listagem Geral de Integrações
    /// </summary>
    /// <remarks>
    /// Recupera todas as integrações (Gateways de Pagamento, ERPs, APIs externas) registradas em todos os sistemas.
    /// Os parâmetros sensíveis (chaves de API) são listados, porém é responsabilidade do front-end mascará-los caso necessário.
    /// </remarks>
    /// <response code="200">Lista completa de integrações e parâmetros.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. Apenas Admin e Dev têm acesso.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        var integrations = await _context.Integrations
            .Include(i => i.Parameters)
            .Select(i => new IntegrationResponseDto
            {
                Id = i.Id,
                AppSystemId = i.AppSystemId,
                Name = i.Name,
                Description = i.Description,
                Type = i.Type,
                Parameters = i.Parameters.Select(p => new IntegrationParameterDto
                {
                    Id = p.Id,
                    Key = p.Key,
                    Value = p.Value
                }).ToList()
            })
            .ToListAsync();

        return Ok(integrations);
    }

    /// <summary>
    /// Listagem de Integrações por Sistema
    /// </summary>
    /// <remarks>
    /// Retorna as integrações ativas atreladas especificamente a um sistema (AppSystem).
    /// </remarks>
    /// <param name="appSystemId">ID do sistema alvo.</param>
    /// <response code="200">Integrações do sistema recuperadas com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet("system/{appSystemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBySystem(int appSystemId)
    {
        var integrations = await _context.Integrations
            .Include(i => i.Parameters)
            .Where(i => i.AppSystemId == appSystemId)
            .Select(i => new IntegrationResponseDto
            {
                Id = i.Id,
                AppSystemId = i.AppSystemId,
                Name = i.Name,
                Description = i.Description,
                Type = i.Type,
                Parameters = i.Parameters.Select(p => new IntegrationParameterDto
                {
                    Id = p.Id,
                    Key = p.Key,
                    Value = p.Value
                }).ToList()
            })
            .ToListAsync();

        return Ok(integrations);
    }

    /// <summary>
    /// Busca de Integração por ID
    /// </summary>
    /// <remarks>
    /// Recupera os dados detalhados e os parâmetros (chaves e valores) de uma integração específica.
    /// </remarks>
    /// <param name="id">ID numérico da integração.</param>
    /// <response code="200">Dados da integração localizados com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">A integração informada não foi localizada.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var integration = await _context.Integrations
            .Include(i => i.Parameters)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (integration == null)
            return NotFound(new { message = "Integração não encontrada." });

        var response = new IntegrationResponseDto
        {
            Id = integration.Id,
            AppSystemId = integration.AppSystemId,
            Name = integration.Name,
            Description = integration.Description,
            Type = integration.Type,
            Parameters = integration.Parameters.Select(p => new IntegrationParameterDto
            {
                Id = p.Id,
                Key = p.Key,
                Value = p.Value
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Cadastro de Nova Integração
    /// </summary>
    /// <remarks>
    /// Cadastra uma integração no sistema, recebendo também a lista de parâmetros (Chave e Valor) necessários para o funcionamento.
    /// É verificado previamente se o sistema informado (AppSystem) existe na base de dados.
    /// </remarks>
    /// <param name="dto">Dados e parâmetros da integração.</param>
    /// <response code="201">Integração criada com sucesso. Retorna o ID gerado.</response>
    /// <response code="400">Payload inválido ou o Sistema (AppSystemId) não existe.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateIntegrationDto dto)
    {
        var systemExists = await _context.AppSystems.AnyAsync(s => s.Id == dto.AppSystemId);
        if (!systemExists)
            return BadRequest(new { message = "Sistema não encontrado." });

        var integration = new Integration
        {
            AppSystemId = dto.AppSystemId,
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Parameters = dto.Parameters.Select(p => new IntegrationParameter
            {
                Key = p.Key,
                Value = p.Value
            }).ToList()
        };

        _context.Integrations.Add(integration);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = integration.Id }, new { message = "Integração criada com sucesso!", id = integration.Id });
    }

    /// <summary>
    /// Atualização e Sincronização de Integração
    /// </summary>
    /// <remarks>
    /// Atualiza os dados principais da integração e realiza uma **sincronização de parâmetros**. 
    /// Parâmetros não enviados no payload serão excluídos; parâmetros sem ID serão criados; parâmetros com ID serão atualizados.
    /// </remarks>
    /// <param name="id">ID numérico da integração.</param>
    /// <param name="dto">Novos dados da integração e lista atualizada de parâmetros.</param>
    /// <response code="200">Integração e parâmetros sincronizados com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">A integração não foi encontrada.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateIntegrationDto dto)
    {
        var integration = await _context.Integrations
            .Include(i => i.Parameters)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (integration == null)
            return NotFound(new { message = "Integração não encontrada." });

        integration.Name = dto.Name;
        integration.Description = dto.Description;
        integration.Type = dto.Type;

        // Atualização dos parâmetros:
        // 1. Remover os que não foram enviados
        var paramIdsReceived = dto.Parameters.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToList();
        var paramsToRemove = integration.Parameters.Where(p => !paramIdsReceived.Contains(p.Id)).ToList();
        foreach (var pToRemove in paramsToRemove)
        {
            _context.IntegrationParameters.Remove(pToRemove);
        }

        // 2. Adicionar ou atualizar
        foreach (var paramDto in dto.Parameters)
        {
            if (paramDto.Id.HasValue && paramDto.Id.Value > 0)
            {
                var existingParam = integration.Parameters.FirstOrDefault(p => p.Id == paramDto.Id.Value);
                if (existingParam != null)
                {
                    existingParam.Key = paramDto.Key;
                    existingParam.Value = paramDto.Value;
                }
            }
            else
            {
                integration.Parameters.Add(new IntegrationParameter
                {
                    Key = paramDto.Key,
                    Value = paramDto.Value
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Integração atualizada com sucesso!" });
    }

    /// <summary>
    /// Exclusão de Integração
    /// </summary>
    /// <remarks>
    /// Deleta fisicamente uma integração do sistema. 
    /// O banco de dados (EF Core) cuida de excluir em cascata todos os parâmetros atrelados a ela.
    /// </remarks>
    /// <param name="id">ID numérico da integração a ser excluída.</param>
    /// <response code="200">Integração e seus parâmetros removidos permanentemente.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">A integração informada não foi localizada.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        var integration = await _context.Integrations.FindAsync(id);
        
        if (integration == null)
            return NotFound(new { message = "Integração não encontrada." });

        _context.Integrations.Remove(integration);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Integração removida com sucesso!" });
    }
}
