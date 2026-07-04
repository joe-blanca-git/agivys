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
    /// Vincula um usuário a um menu.
    /// </summary>
    /// <response code="200">Vínculo criado com sucesso.</response>
    /// <response code="400">Erro nos dados enviados.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (Owner necessário).</response>
    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// Remove o vínculo de um usuário com um menu.
    /// </summary>
    /// <response code="200">Vínculo removido com sucesso.</response>
    /// <response code="400">Erro nos dados enviados.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão (Owner necessário).</response>
    [HttpPut("remove")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// Obtém os mapas de acesso (Sistemas, Menus e Submenus) do usuário logado.
    /// </summary>
    /// <response code="200">Retorna os acessos do usuário.</response>
    /// <response code="401">Não autenticado.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
}
