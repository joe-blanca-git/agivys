using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.DTOs.RLS;
using AgiVysSystem.Api.Models.User;
using Asp.Versioning;

namespace AgiVysSystem.Api.Controllers.RLS;

/// <summary>
/// Controlador responsável pelo gerenciamento de Roles (Role Level Security).
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(Roles = "Dev,Admin")]
public class RLSController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public RLSController(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Retorna a lista de todas as roles cadastradas.
    /// </summary>
    /// <returns>Lista com Id e Name das roles</returns>
    [HttpGet("getRoles")]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty
            })
            .ToListAsync();

        return Ok(roles);
    }

    /// <summary>
    /// Vincula um usuário a uma role.
    /// </summary>
    /// <param name="dto">Dados para vincular a role</param>
    /// <returns>Mensagem de sucesso ou erro</returns>
    [HttpPost("postAssignRole")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto dto)
    {
        if (dto.UserId <= 0)
            return BadRequest("UserId é obrigatório e deve ser maior que zero.");

        if (dto.RoleId == null && string.IsNullOrEmpty(dto.RoleName))
            return BadRequest("É obrigatório informar RoleId ou RoleName.");

        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user == null)
            return NotFound("Usuário não encontrado.");

        IdentityRole<int>? role = null;
        if (dto.RoleId.HasValue)
            role = await _roleManager.FindByIdAsync(dto.RoleId.Value.ToString());
        else if (!string.IsNullOrEmpty(dto.RoleName))
            role = await _roleManager.FindByNameAsync(dto.RoleName);

        if (role == null || string.IsNullOrEmpty(role.Name))
            return NotFound("Role não encontrada.");

        var isInRole = await _userManager.IsInRoleAsync(user, role.Name);
        if (isInRole)
            return BadRequest("Usuário já possui esta role vinculada.");

        var result = await _userManager.AddToRoleAsync(user, role.Name);
        if (!result.Succeeded)
            return BadRequest($"Erro ao vincular role: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return Ok("Role vinculada com sucesso.");
    }

    /// <summary>
    /// Remove uma role de um usuário.
    /// </summary>
    /// <param name="dto">Dados para remover a role</param>
    /// <returns>Mensagem de sucesso ou erro</returns>
    [HttpDelete("removeRole")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequestDto dto)
    {
        if (dto.UserId <= 0)
            return BadRequest("UserId é obrigatório e deve ser maior que zero.");

        if (dto.RoleId == null && string.IsNullOrEmpty(dto.RoleName))
            return BadRequest("É obrigatório informar RoleId ou RoleName.");

        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user == null)
            return NotFound("Usuário não encontrado.");

        IdentityRole<int>? role = null;
        if (dto.RoleId.HasValue)
            role = await _roleManager.FindByIdAsync(dto.RoleId.Value.ToString());
        else if (!string.IsNullOrEmpty(dto.RoleName))
            role = await _roleManager.FindByNameAsync(dto.RoleName);

        if (role == null || string.IsNullOrEmpty(role.Name))
            return NotFound("Role não encontrada.");

        var isInRole = await _userManager.IsInRoleAsync(user, role.Name);
        if (!isInRole)
            return BadRequest("O usuário não possui esta role vinculada.");

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
        if (!result.Succeeded)
            return BadRequest($"Erro ao remover role: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return Ok("Role removida com sucesso.");
    }
}
