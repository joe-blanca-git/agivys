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
    /// Lista todas as integrações cadastradas.
    /// </summary>
    [HttpGet]
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
    /// Lista integrações vinculadas a um sistema específico.
    /// </summary>
    [HttpGet("system/{appSystemId}")]
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
    /// Busca uma integração específica pelo ID.
    /// </summary>
    [HttpGet("{id}")]
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
    /// Cria uma nova integração e seus parâmetros.
    /// </summary>
    [HttpPost]
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
    /// Atualiza os dados de uma integração e sincroniza seus parâmetros.
    /// </summary>
    [HttpPut("{id}")]
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
    /// Exclui uma integração (os parâmetros são removidos em cascata automaticamente).
    /// </summary>
    [HttpDelete("{id}")]
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
