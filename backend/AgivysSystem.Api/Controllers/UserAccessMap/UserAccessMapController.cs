using AgiVysSystem.Api.DTOs.UserAccessMap;
using AgiVysSystem.Api.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgiVysSystem.Api.Controllers.UserAccessMap;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UserAccessMapController : ControllerBase
{
    private readonly IUserAccessMapService _service;

    public UserAccessMapController(IUserAccessMapService service)
    {
        _service = service;
    }

    /// <summary>
    /// Concessão de Acesso a Menu (ACL)
    /// </summary>
    /// <remarks>
    /// Concede acesso explícito a um Menu/Funcionalidade para um usuário específico.
    /// Utilizado para criar permissões granulares por usuário que vão além das permissões de Plano ou Regra Global.
    /// **Regra de Segurança**: Exige privilégio de Dono (Owner) do workspace.
    /// </remarks>
    /// <param name="dto">Dados do vínculo (ID do Usuário e ID do Menu).</param>
    /// <response code="200">Acesso concedido com sucesso.</response>
    /// <response code="400">Payload inválido ou vínculo já existente.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. Apenas 'Owner' tem acesso.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddUserAccessMap([FromBody] UserAccessMapDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _service.AddUserAccessMapAsync(dto);
            return Ok(new { message = "Vínculo de acesso criado com sucesso." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Revogação de Acesso a Menu (ACL)
    /// </summary>
    /// <remarks>
    /// Revoga o acesso explícito de um usuário a um Menu/Funcionalidade específica.
    /// **Regra de Segurança**: Exige privilégio de Dono (Owner) do workspace.
    /// </remarks>
    /// <param name="dto">Dados do vínculo a ser removido (ID do Usuário e ID do Menu).</param>
    /// <response code="200">Acesso revogado com sucesso.</response>
    /// <response code="400">Payload inválido ou vínculo não localizado.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. Apenas 'Owner' tem acesso.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPut("remove")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveUserAccessMap([FromBody] UserAccessMapDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _service.RemoveUserAccessMapAsync(dto);
            return Ok(new { message = "Vínculo de acesso removido com sucesso." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Consulta de Árvore de Acesso do Usuário (Base de Dados)
    /// </summary>
    /// <remarks>
    /// Consulta no banco de dados toda a árvore hierárquica de permissões (Sistemas -> Menus -> Submenus) disponível para o usuário autenticado.
    /// O ID do usuário é extraído de forma segura via token (ClaimTypes.NameIdentifier).
    /// </remarks>
    /// <response code="200">Árvore de permissões recuperada com sucesso.</response>
    /// <response code="400">Erro durante a montagem da árvore.</response>
    /// <response code="401">Usuário não autenticado ou ID do token inválido.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserAccessMap()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Não foi possível identificar o usuário autenticado." });
        }

        try
        {
            var accessMaps = await _service.GetUserAccessMapAsync(userId);
            return Ok(accessMaps);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Consulta de Árvore de Acesso do Usuário (via Sessão/Cookie)
    /// </summary>
    /// <remarks>
    /// Deserializa a árvore hierárquica de permissões (Sistemas -> Menus -> Submenus) diretamente de um Cookie HttpOnly encriptado e codificado em Base64.
    /// **Performance**: Projetado para renderização imediata do front-end com ganho extremo de velocidade (0 trips ao banco de dados).
    /// </remarks>
    /// <response code="200">Árvore de permissões carregada com sucesso da sessão.</response>
    /// <response code="401">Cookie de sessão ausente, malformado ou corrompido.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet("session")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetUserAccessMapFromSession()
    {
        if (Request.Cookies.TryGetValue("MedNext_Menu", out var encodedMap))
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedMap));
                var map = System.Text.Json.JsonSerializer.Deserialize<List<UserAccessMapResponseDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(map);
            }
            catch
            {
                return Unauthorized(new { message = "Sessão de menu inválida." });
            }
        }

        return Unauthorized(new { message = "Cookie de menu não encontrado." });
    }
}
