using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs;
using AgiVysSystem.Api.Models.User;
using AgiVysSystem.Api.Interfaces;
using Asp.Versioning;

namespace AgiVysSystem.Api.Controllers.Authentication.V2;

/// <summary>
/// Autenticação (v2) — o JWT nunca aparece no corpo da resposta nem é lido pelo
/// JavaScript do front: ele viaja num cookie HttpOnly, e um segundo cookie
/// (legível) carrega um valor de CSRF que o front deve ecoar no header
/// <c>X-CSRF-Token</c> em toda requisição que altera dado.
/// </summary>
/// <remarks>
/// Rota isolada da v1 — não altera nem substitui <c>api/v1/authentication</c>.
/// </remarks>
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private const string AccessTokenCookie = "agivys_at";
    private const string CsrfCookie = "agivys_csrf";
    private const string MenuCookie = "MedNext_Menu";

    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserAccessMapService _userAccessMapService;

    public AuthenticationController(
        UserManager<User> userManager,
        AppDbContext context,
        IJwtTokenService jwtTokenService,
        IUserAccessMapService userAccessMapService)
    {
        _userManager = userManager;
        _context = context;
        _jwtTokenService = jwtTokenService;
        _userAccessMapService = userAccessMapService;
    }

    /// <summary>
    /// Autenticação de Usuário (Login) — v2, baseada em cookie.
    /// </summary>
    /// <remarks>
    /// Mesma validação de credenciais da v1. A diferença é a resposta: o JWT é gravado
    /// direto num cookie `HttpOnly` (`agivys_at`), e não aparece em nenhum campo do JSON.
    /// Um segundo cookie legível (`agivys_csrf`) precisa ser ecoado pelo front no header
    /// `X-CSRF-Token` em toda chamada que não seja GET/HEAD/OPTIONS — é a proteção contra
    /// CSRF que a autenticação por header dispensava.
    ///
    /// Exige HTTPS: o cookie é `Secure`, então só é gravado/enviado em conexões `https://`.
    /// </remarks>
    /// <param name="model">Credenciais de acesso (E-mail e Senha).</param>
    /// <response code="200">Login realizado com sucesso. Cookies gravados; corpo só com dados do usuário.</response>
    /// <response code="401">E-mail ou senha incorretos.</response>
    /// <response code="500">Erro interno do servidor ao tentar autenticar.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseV2Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            return Unauthorized(new { message = "E-mail ou senha incorretos." });
        }

        await _context.Entry(user).Collection(u => u.UserSystems).Query().Include(us => us.AppSystem).LoadAsync();

        var person = await _context.People.FirstOrDefaultAsync(p => p.Id == user.PersonId);
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserOwnerId == user.Id);
        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenService.GenerateToken(user, roles, person?.Name);
        var expiration = DateTime.UtcNow.AddHours(4);

        var response = new LoginResponseV2Dto
        {
            Expiration = expiration,
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email!,
                CompanyId = company?.Id,
                CompanyName = company?.Name,
                Roles = roles.Select(r => new AuthUserRoleDto
                {
                    Name = "UserType",
                    Value = r
                }).ToList()
            },
            Person = new AuthPersonDto
            {
                Id = person?.Id,
                Name = person?.Name,
                Email = person?.Email
            },
            SystemIds = user.UserSystems.Select(us => us.AppSystemId).ToList(),
            SystemNames = user.UserSystems.Select(us => us.AppSystem.Name).ToList()
        };

        Response.Cookies.Append(AccessTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiration
        });

        // Legível de propósito: o front lê esse valor e o devolve no header X-CSRF-Token
        // (dupla submissão). Sem o cookie agivys_at, esse valor sozinho não autentica nada.
        Response.Cookies.Append(CsrfCookie, Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiration
        });

        var accessMap = await _userAccessMapService.GetUserAccessMapAsync(user.Id);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var accessMapJson = JsonSerializer.Serialize(accessMap, options);
        var encodedMap = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessMapJson));

        Response.Cookies.Append(MenuCookie, encodedMap, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiration
        });

        return Ok(response);
    }

    /// <summary>
    /// Sessão Atual ("quem sou eu") — v2.
    /// </summary>
    /// <remarks>
    /// Devolve o perfil de quem já está autenticado pelo cookie <c>agivys_at</c> (ou por
    /// Bearer, se vier). O front chama essa rota uma vez ao carregar a página pra saber se
    /// a sessão ainda vale — sem isso, não haveria como confirmar login num F5 ou aba nova,
    /// já que o JWT em si nunca chega no JavaScript.
    /// </remarks>
    /// <response code="200">Sessão válida — devolve os dados do usuário.</response>
    /// <response code="401">Sem sessão válida (sem cookie/token, ou expirado).</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(LoginResponseV2Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return Unauthorized();

        await _context.Entry(user).Collection(u => u.UserSystems).Query().Include(us => us.AppSystem).LoadAsync();

        var person = await _context.People.FirstOrDefaultAsync(p => p.Id == user.PersonId);
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserOwnerId == user.Id);
        var roles = await _userManager.GetRolesAsync(user);

        var response = new LoginResponseV2Dto
        {
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email!,
                CompanyId = company?.Id,
                CompanyName = company?.Name,
                Roles = roles.Select(r => new AuthUserRoleDto
                {
                    Name = "UserType",
                    Value = r
                }).ToList()
            },
            Person = new AuthPersonDto
            {
                Id = person?.Id,
                Name = person?.Name,
                Email = person?.Email
            },
            SystemIds = user.UserSystems.Select(us => us.AppSystemId).ToList(),
            SystemNames = user.UserSystems.Select(us => us.AppSystem.Name).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Encerramento de Sessão (Logout) — v2.
    /// </summary>
    /// <remarks>
    /// Como os cookies de sessão são HttpOnly, só a API consegue removê-los —
    /// o front não tem como "esquecer" o token sozinho, precisa chamar essa rota.
    /// </remarks>
    /// <response code="200">Logout realizado com sucesso. Cookies removidos.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
        Response.Cookies.Delete(CsrfCookie, new CookieOptions { HttpOnly = false, Secure = true, SameSite = SameSiteMode.None });
        Response.Cookies.Delete(MenuCookie, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });

        return Ok(new { message = "Logout realizado com sucesso." });
    }
}
