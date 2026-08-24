using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs;
using AgiVysSystem.Api.Models.User;
using AgiVysSystem.Api.Models.People;
using AgiVysSystem.Api.Models.Configuration;
using AgiVysSystem.Api.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace AgiVysSystem.Api.Controllers.Authentication;

/// <summary>
/// Controlador responsável pela autenticação e gestão de acessos do ecossistema AgiVysSystem.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IUserAccessMapService _userAccessMapService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationController(
        UserManager<User> userManager,
        AppDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        IUserAccessMapService userAccessMapService,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _userAccessMapService = userAccessMapService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Validação de Token JWT
    /// </summary>
    /// <remarks>
    /// Recebe um token JWT e verifica sua assinatura, emissor, audiência e validade.
    /// Utilizado internamente ou por microsserviços para confirmar se um token ainda é válido.
    /// </remarks>
    /// <param name="dto">Objeto contendo o token JWT a ser validado.</param>
    /// <response code="200">Retorna true se o token for válido, false caso contrário.</response>
    /// <response code="400">Token não fornecido ou nulo.</response>
    /// <response code="500">Erro interno ao processar a validação.</response>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ValidateToken([FromBody] TokenValidationRequest dto)
    {
        if (string.IsNullOrEmpty(dto.Token))
            return BadRequest(false);

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "");

            tokenHandler.ValidateToken(dto.Token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true, // Verifica se o token expirou
                ClockSkew = TimeSpan.Zero // Remove a tolerância padrão de 5 min para ser exato
            }, out SecurityToken validatedToken);

            // Se chegou aqui sem lançar exceção, o token é válido
            return Ok(true);
        }
        catch (Exception)
        {
            // Se der erro na validação (token expirado, chave errada, etc)
            return Ok(false);
        }
    }

    // DTO simples para receber o JSON do Python
    public class TokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Autenticação de Usuário (Login)
    /// </summary>
    /// <remarks>
    /// Valida as credenciais do usuário e, se corretas, retorna um Token JWT junto com os dados essenciais do perfil e permissões.
    /// 
    /// **Detalhes Importantes:**
    /// - O token gerado possui validade de **4 horas**.
    /// - O token deve ser enviado no cabeçalho `Authorization: Bearer {token}` em requisições subsequentes.
    /// - Além do token, a API também pode gerar um cookie `MedNext_Menu` (HttpOnly) contendo as permissões.
    /// </remarks>
    /// <param name="model">Credenciais de acesso (E-mail e Senha).</param>
    /// <response code="200">Login realizado com sucesso. Retorna o Token e dados do usuário.</response>
    /// <response code="401">E-mail ou senha incorretos.</response>
    /// <response code="500">Erro interno do servidor ao tentar autenticar.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            return Unauthorized(new { message = "E-mail ou senha incorretos." });
        }

        // Carregamento eficiente das coleções N:N
        await _context.Entry(user).Collection(u => u.UserSystems).Query().Include(us => us.AppSystem).LoadAsync();

        var person = await _context.People.FirstOrDefaultAsync(p => p.Id == user.PersonId);
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserOwnerId == user.Id);
        var roles = await _userManager.GetRolesAsync(user);

        var token = _jwtTokenService.GenerateToken(user, roles, person?.Name);

        var response = new LoginResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddHours(4),
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

        // Obter mapa de acesso e salvar no Cookie HttpOnly
        var accessMap = await _userAccessMapService.GetUserAccessMapAsync(user.Id);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var accessMapJson = JsonSerializer.Serialize(accessMap, options);
        var encodedMap = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessMapJson));

        Response.Cookies.Append("MedNext_Menu", encodedMap, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Obrigatório para SameSite=None
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(4)
        });

        return Ok(response);
    }

    /// <summary>
    /// Encerramento de Sessão (Logout)
    /// </summary>
    /// <remarks>
    /// Realiza o logout do usuário limpando os cookies de sessão, em especial o cookie `MedNext_Menu`.
    /// Observe que, como o JWT é stateless, o cliente também deve descartar o token do lado do front-end.
    /// </remarks>
    /// <response code="200">Logout realizado com sucesso. Cookies removidos.</response>
    /// <response code="500">Erro interno ao processar o logout.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("MedNext_Menu", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
        return Ok(new { message = "Logout realizado com sucesso." });
    }

    /// <summary>
    /// Registro de Proprietário de Sistema
    /// </summary>
    /// <remarks>
    /// Cria um novo usuário com a role "Owner" (Proprietário) vinculado a um sistema específico (`IdSystem`).
    /// 
    /// **Regras de Negócio:**
    /// - O E-mail fornecido não pode existir previamente na base de dados.
    /// - O ID do sistema deve corresponder a um registro válido.
    /// - Um documento fictício único é gerado automaticamente.
    /// - Após o sucesso, um e-mail de boas-vindas com as cores do sistema é enviado.
    /// - A senha deve obedecer a política de segurança (letras, números e caracteres especiais).
    /// </remarks>
    /// <param name="model">Dados necessários para criar a conta (Nome, E-mail, Senha, Sistema, etc).</param>
    /// <response code="200">Usuário cadastrado com sucesso.</response>
    /// <response code="400">Dados inválidos, e-mail já em uso, sistema inexistente ou senha fraca.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPost("register-system-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterSystemUser([FromBody] RegisterSystemUserDto model)
    {
        var system = await _context.AppSystems.FindAsync(model.IdSystem);
        if (system == null)
            return BadRequest(new { message = "Dados inválidos! idSystem não corresponde a um sistema válido." });

        var userExists = await _userManager.FindByEmailAsync(model.Email);

        // E-mail já tem conta: a mesma pessoa pode ganhar acesso a outro sistema sem
        // duplicar a conta (o vínculo user<->sistema é N:N). O que continua proibido é
        // duplicar exatamente o par e-mail + sistema, e por isso exigimos a senha atual
        // aqui — sem isso, qualquer um poderia "registrar" um e-mail que não é seu só
        // pra ganhar acesso à conta de outra pessoa num sistema novo.
        if (userExists != null)
        {
            if (!await _userManager.CheckPasswordAsync(userExists, model.Password))
            {
                return BadRequest(new
                {
                    message = "Este e-mail já possui uma conta. Informe a senha atual para vincular este sistema, ou use \"Esqueci minha senha\"."
                });
            }

            var alreadyLinkedToThisSystem = await _context.UserSystems
                .AnyAsync(us => us.UserId == userExists.Id && us.AppSystemId == model.IdSystem);

            if (alreadyLinkedToThisSystem)
                return BadRequest(new { message = "Este e-mail já está cadastrado neste sistema." });

            _context.UserSystems.Add(new UserSystem { UserId = userExists.Id, AppSystemId = model.IdSystem });

            if (!await _userManager.IsInRoleAsync(userExists, "Owner"))
                await _userManager.AddToRoleAsync(userExists, "Owner");

            await _context.SaveChangesAsync();

            var existingPerson = await _context.People.FindAsync(userExists.PersonId);
            await SendWelcomeEmailAsync(system, userExists.Email!, existingPerson?.Name ?? model.Name, model.IdSystem);

            return Ok(new { message = "Conta existente vinculada ao novo sistema com sucesso!" });
        }

        // E-mail novo: fluxo de cadastro normal (Person + User + UserSystem + Role).
        // O documento (CPF) não é mais obrigatório no cadastro — deixamos sem
        // preencher para que seja null.
        var person = new Models.People.Person
        {
            Name = model.Name,
            Email = model.Email,
            BirthDate = model.BirthDate,
            Phone = model.Phone
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync(); // Gerar ID da Person

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            PersonId = person.Id
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _context.UserSystems.Add(new UserSystem { UserId = user.Id, AppSystemId = model.IdSystem });

            await _userManager.AddToRoleAsync(user, "Owner");

            // Atualiza a Person com o UserId gerado
            person.UserId = user.Id;
            await _context.SaveChangesAsync();

            await SendWelcomeEmailAsync(system, model.Email, model.Name, model.IdSystem);

            return Ok(new { message = "Usuário cadastrado com sucesso!" });
        }

        // Caso a criação do User falhe (ex: senha fraca), removemos a Person para evitar lixo no banco
        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        return BadRequest(result.Errors);
    }

    private async Task SendWelcomeEmailAsync(AppSystem system, string email, string personName, int idSystem)
    {
        var sysName = system.Name;
        var sysBgColor = system.BackgroundColor ?? "#1a1a1a";
        var sysTxtColor = system.TextColor ?? "#ffffff";
        var sysDomain = system.Domain ?? "agivyssystem.com.br";
        var sysUrl = $"https://{sysDomain}/portal-pat/auth/login";

        var welcomeMessage = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden;'>
            <div style='background-color: {sysBgColor}; padding: 20px; text-align: center;'>
                <h1 style='color: {sysTxtColor}; margin: 0; font-size: 24px;'>{sysName}</h1>
            </div>
            <div style='padding: 30px; color: #333; line-height: 1.6;'>
                <h2 style='color: #1a1a1a;'>Olá, {personName}!</h2>
                <p>Seja muito bem-vindo ao <strong>{sysName}</strong>. Seu cadastro foi realizado com sucesso.</p>
                <div style='margin: 30px 0; text-align: center;'>
                    <a href='{sysUrl}' style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Acessar Painel</a>
                </div>
                <hr style='border: 0; border-top: 1px solid #eee;' />
                <p style='font-size: 12px; color: #777;'>&copy; {DateTime.Now.Year} {sysName}.</p>
            </div>
        </div>";

        await _emailService.SendEmailAsync(email, "Bem-vindo ao AgiVys System", welcomeMessage, idSystem);
    }

    /// <summary>
    /// Solicitação de Recuperação de Senha
    /// </summary>
    /// <remarks>
    /// Envia um e-mail contendo um link seguro para redefinição de senha, caso o e-mail exista na base.
    /// 
    /// **Comportamento de Segurança:**
    /// - Para evitar enumeração de contas, a API sempre retorna 200 OK informando que "Se o e-mail existir, o link foi enviado", mesmo que o e-mail não seja encontrado.
    /// - O link gerado expira em 2 horas.
    /// </remarks>
    /// <param name="model">E-mail do usuário e opcionalmente o ID do sistema para customização do template.</param>
    /// <response code="200">Requisição processada com sucesso (e-mail enviado se a conta existir).</response>
    /// <response code="500">Erro interno ao gerar o token ou enviar o e-mail.</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) 
            return Ok(new { message = "Se o e-mail existir em nossa base, um link de recuperação será enviado." });

        // Gera o Token de recuperação (Identity gera um token seguro)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // Codifica o token em Base64Url para evitar problemas com caracteres especiais (+, =) em URLs
        var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // Puxar as configurações do sistema para o template (fallback seguro caso IdSystem seja nulo)
        var system = model.IdSystem.HasValue ? await _context.AppSystems.FindAsync(model.IdSystem.Value) : null;
        var sysName = system?.Name ?? "Sistemas Agivys";
        var sysBgColor = system?.BackgroundColor ?? "#4451c4ff";
        var sysTxtColor = system?.TextColor ?? "#ffffff";
        var sysDomain = system?.Domain ?? "agivyssystem.com.br";

        // Link para o Frontend usando o domínio do sistema
        var callbackUrl = $"https://{sysDomain}/portal-pat/auth/reset-password?key={encodedToken}&email={user.Email}";

        var resetMessage = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden;'>
            <div style='background-color: {sysBgColor}; padding: 20px; text-align: center;'>
                <h1 style='color: {sysTxtColor}; margin: 0; font-size: 24px;'>{sysName}</h1>
            </div>
            <div style='padding: 30px; color: #333; line-height: 1.6;'>
                <h2 style='color: #1a1a1a;'>Recuperação de Senha</h2>
                <p>Olá, {user.UserName}!</p>
                <p>Recebemos uma solicitação para redefinir a sua senha no <strong>{sysName}</strong>.</p>
                <p>Este link é válido por <strong>2 horas</strong>. Se você não solicitou esta alteração, ignore este e-mail.</p>
                
                <div style='margin: 30px 0; text-align: center;'>
                    <a href='{callbackUrl}' style='background-color: #d9534f; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Redefinir Senha</a>
                </div>

                <hr style='border: 0; border-top: 1px solid #eee;' />
                <p style='font-size: 12px; color: #777;'>Por segurança, nunca compartilhe este link com ninguém.</p>
            </div>
            <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #999;'>
                &copy; {DateTime.Now.Year} Sistemas Agivys - Todos os direitos reservados.
            </div>
        </div>";

        await _emailService.SendEmailAsync(user.Email!, $"Recuperação de Senha - {sysName}", resetMessage, model.IdSystem);

        return Ok(new { message = "Link de recuperação enviado com sucesso." });
    }

    /// <summary>
    /// Redefinição de Senha
    /// </summary>
    /// <remarks>
    /// Processa o token gerado na recuperação de senha e define a nova senha fornecida.
    /// O token enviado pelo cliente deve estar em formato `Base64Url`.
    /// </remarks>
    /// <param name="model">E-mail, token de recuperação e a nova senha.</param>
    /// <response code="200">Senha atualizada com sucesso.</response>
    /// <response code="400">Usuário não encontrado, token inválido, expirado, ou nova senha fraca.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) 
            return BadRequest(new { message = "Dados inválidos." });

        // Decodifica o token do formato Base64Url
        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(model.Token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Token inválido ou mal formatado." });
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

        if (result.Succeeded)
        {
            return Ok(new { message = "Senha atualizada com sucesso!" });
        }

        var erros = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(new { message = "Erro ao resetar senha.", detalhes = erros });
    }

    /// <summary>
    /// Alteração de Senha do Usuário Logado
    /// </summary>
    /// <remarks>
    /// Permite que um usuário autenticado mude sua própria senha.
    /// É obrigatório informar a senha atual para garantir a segurança da operação.
    /// </remarks>
    /// <param name="model">Objeto contendo a senha atual e a nova senha.</param>
    /// <response code="200">Senha alterada com sucesso.</response>
    /// <response code="400">Senha atual incorreta ou nova senha não atende aos requisitos.</response>
    /// <response code="401">Usuário não autenticado (Token JWT ausente ou inválido).</response>
    /// <response code="404">Registro do usuário autenticado não foi encontrado no banco de dados.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) 
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) 
            return Unauthorized(new { message = "Usuário não encontrado." });

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            return Ok(new { message = "Senha alterada com sucesso!" });
        }

        var erros = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(new { message = "Erro ao alterar a senha.", detalhes = erros });
    }
}
