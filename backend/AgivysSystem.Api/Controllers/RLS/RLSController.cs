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
    /// Listagem de Regras de Acesso (Roles)
    /// </summary>
    /// <remarks>
    /// Recupera todas as regras de controle de acesso (Roles) cadastradas no sistema Identity.
    /// Endpoint restrito a perfis de administração (Admin e Dev).
    /// </remarks>
    /// <response code="200">Lista de roles recuperada com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Atribuição de Regra (Role) a Usuário
    /// </summary>
    /// <remarks>
    /// Vincula uma Regra de Acesso (Role) a um Usuário específico. O vínculo pode ser feito através do `RoleId` numérico ou diretamente pelo `RoleName` textual.
    /// É verificado previamente se o usuário já possui a regra atribuída para evitar duplicidade.
    /// </remarks>
    /// <param name="dto">Objeto contendo o ID do usuário e a identificação da Role (Id ou Nome).</param>
    /// <response code="200">Regra atribuída ao usuário com sucesso.</response>
    /// <response code="400">Payload inválido, IDs nulos ou o usuário já possui a Role.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="404">Usuário ou Role informada não localizada na base de dados.</response>
    /// <response code="500">Erro interno do Identity ao tentar realizar o vínculo.</response>
    [HttpPost("assignrole")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Remoção de Regra (Role) de um Usuário
    /// </summary>
    /// <remarks>
    /// Desvincula uma Regra de Acesso de um usuário. O vínculo pode ser removido utilizando o `RoleId` numérico ou o `RoleName` textual.
    /// É verificado previamente se o usuário realmente possui esta regra vinculada.
    /// </remarks>
    /// <param name="dto">Objeto contendo o ID do usuário e a identificação da Role (Id ou Nome).</param>
    /// <response code="200">Regra removida do usuário com sucesso.</response>
    /// <response code="400">Payload inválido, IDs nulos ou o usuário não possui a Role informada.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="404">Usuário ou Role informada não localizada na base de dados.</response>
    /// <response code="500">Erro interno do Identity ao tentar desfazer o vínculo.</response>
    [HttpDelete("removerole")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Cadastro de Nova Regra (Role)
    /// </summary>
    /// <remarks>
    /// Cria uma nova regra global (Role) no sistema (ex: Financeiro, RH, Suporte).
    /// **Regra de Segurança**: Esta operação estrutural é restrita apenas a perfis de Desenvolvedor (Dev).
    /// </remarks>
    /// <param name="dto">Objeto contendo o Nome da nova role.</param>
    /// <response code="200">Role criada e registrada com sucesso no Identity.</response>
    /// <response code="400">Payload em branco, nulo ou nome da Role já existente.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. Apenas perfis 'Dev' podem criar novas regras estruturais.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize(Roles = "Dev")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "O nome da role é obrigatório." });

        var roleExists = await _roleManager.RoleExistsAsync(dto.Name);
        if (roleExists)
            return BadRequest(new { message = "Já existe uma role com este nome." });

        var result = await _roleManager.CreateAsync(new IdentityRole<int>(dto.Name));
        
        if (result.Succeeded)
            return Ok(new { message = "Role criada com sucesso." });

        return BadRequest(new { message = $"Erro ao criar role: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
    }

    /// <summary>
    /// Atualização de Nome de Regra (Role)
    /// </summary>
    /// <remarks>
    /// Altera o nome de uma Role existente. Essa alteração é propagada globalmente no sistema de permissões.
    /// **Regra de Segurança**: Restrito a perfis de Desenvolvedor (Dev).
    /// </remarks>
    /// <param name="dto">Objeto contendo o ID numérico e o Novo Nome da role.</param>
    /// <response code="200">Nome da role atualizado com sucesso.</response>
    /// <response code="400">Payload inválido ou o novo nome já está em uso por outra Role.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. Apenas perfis 'Dev' podem editar regras.</response>
    /// <response code="404">Role informada não encontrada na base de dados.</response>
    /// <response code="500">Erro interno do Identity ao tentar atualizar a Role.</response>
    [Authorize(Roles = "Dev")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EditRole([FromBody] RoleDto dto)
    {
        if (dto.Id <= 0 || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "O Id e o novo Nome da role são obrigatórios." });

        var role = await _roleManager.FindByIdAsync(dto.Id.ToString());
        if (role == null)
            return NotFound(new { message = "Role não encontrada." });

        var roleWithSameName = await _roleManager.FindByNameAsync(dto.Name);
        if (roleWithSameName != null && roleWithSameName.Id != role.Id)
            return BadRequest(new { message = "Já existe outra role com este mesmo nome." });

        role.Name = dto.Name;
        var result = await _roleManager.UpdateAsync(role);

        if (result.Succeeded)
            return Ok(new { message = "Role atualizada com sucesso." });

        return BadRequest(new { message = $"Erro ao atualizar role: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
    }
}
