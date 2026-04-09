using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs;
using AgiVysSystem.Api.Models.User;
using AgiVysSystem.Api.Models.People;
using AgiVysSystem.Api.Models.Company;
using AgiVysSystem.Api.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace AgiVysSystem.Api.Controllers.Auth;

/// <summary>
/// Controlador responsável pela autenticação e gestão de acessos do ecossistema AgiVysSystem.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthController(UserManager<User> userManager, AppDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    [HttpPost("validate-token")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
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
    /// Autentica um usuário e retorna o Token JWT junto com os dados do perfil e permissões.
    /// </summary>
    /// <remarks>
    /// O token gerado tem validade de 4 horas e deve ser enviado no Header de todas as requisições protegidas.
    /// </remarks>
    /// <param name="model">Credenciais de acesso (Email e Senha).</param>
    /// <response code="200">Login realizado com sucesso.</response>
    /// <response code="401">E-mail ou senha incorretos.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            return Unauthorized(new { message = "E-mail ou senha incorretos." });
        }

        // Busca os dados da Pessoa vinculada
        var person = await _context.People.FirstOrDefaultAsync(p => p.Id == user.PersonId);

        // Busca os dados da Empresa vinculada
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserOwnerId == user.Id);
        
        // Busca as Roles do usuário
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "NoRole";

        // Geração do Token JWT (4 horas)
        var token = GenerateJwtToken(user, primaryRole);

        return Ok(new
        {
            token,
            expiration = DateTime.UtcNow.AddHours(4),
            user = new
            {
                id = user.Id,
                email = user.Email,
                companyId = company?.Id,
                companyName = company?.Name,
                role = new 
                { 
                    name = "UserType", 
                    value = primaryRole 
                }
            },
            person = new
            {
                id = person?.Id,
                name = person?.Name,
                email = person?.Email
            }
        });
    }

    private string GenerateJwtToken(User user, string role)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(4),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Realiza o cadastro de um novo usuário do tipo Proprietário no sistema.
    /// </summary>
    /// <param name="model">A Senha deve conter letras, números e ao menos um caractere especial.</param>
    /// <returns>Retorna uma mensagem de sucesso ou uma lista de erros de validação.</returns>
    /// <response code="200">Usuário Cadastrado com Sucesso!</response>
    /// <response code="400">Dados inválidos.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        // 1. Validações de existência
        var userExists = await _userManager.FindByEmailAsync(model.Email);
        if (userExists != null)
            return BadRequest(new { message = "Dados Inválidos" });

        var documentExists = await _context.People.AnyAsync(p => p.Document == model.Document);
        if (documentExists)
            return BadRequest(new { message = "Dados Inválidos" });

        // 2. Criar a Pessoa (People)
        var person = new Person
        {
            Name = model.Name,
            Document = model.Document,
            Email = model.Email,
            BirthDate = model.BirthDate
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync(); // Gerar ID da Person

        // 3. Criar o Usuário vinculado à Pessoa
        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            PersonId = person.Id
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // 4. Atribuir a Role "Owner"
            await _userManager.AddToRoleAsync(user, "Owner");

            // Atualiza a Person com o UserId gerado
            person.UserId = user.Id;

            // 5. Criar o Endereço Inicial
            var initialAddress = new AddressPerson
            {
                PersonId = person.Id,
                Description = model.AddressDescription ?? "Principal",
                ZipCode = model.ZipCode,
                Street = model.Street,
                Number = model.Number,
                Complement = model.Complement,
                Neighborhood = model.Neighborhood,
                City = model.City,
                State = model.State
            };

            _context.AddressPeople.Add(initialAddress);
            await _context.SaveChangesAsync();

            // 6. E-mail de Boas-vindas (Template atualizado para AgiVysSystem)
            var welcomeMessage = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #1a1a1a; padding: 20px; text-align: center;'>
                    <h1 style='color: #ffffff; margin: 0; font-size: 24px;'>AgiVys System</h1>
                </div>
                <div style='padding: 30px; color: #333; line-height: 1.6;'>
                    <h2 style='color: #1a1a1a;'>Olá, {model.Name}!</h2>
                    <p>Seja muito bem-vindo ao <strong>AgiVys System</strong>. Seu cadastro foi realizado com sucesso.</p>
                    <p>Seu endereço principal em <strong>{model.City}/{model.State}</strong> também já foi configurado.</p>
                    <div style='margin: 30px 0; text-align: center;'>
                        <a href='https://agivyssystem.com.br/login' style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Acessar Painel</a>
                    </div>
                    <hr style='border: 0; border-top: 1px solid #eee;' />
                    <p style='font-size: 12px; color: #777;'>&copy; {DateTime.Now.Year} AgiVys System.</p>
                </div>
            </div>";

            await _emailService.SendEmailAsync(model.Email, "Bem-vindo ao AgiVys System", welcomeMessage);

            return Ok(new { message = "Usuário cadastrado com sucesso!" });
        }

        // Caso a criação do User falhe (ex: senha fraca), removemos a Person para evitar lixo no banco
        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Atualiza os dados pessoais do usuário autenticado.
    /// </summary>
    /// <response code="200">Dados atualizados com sucesso.</response>
    /// <response code="403">Tentativa de editar dados de outro usuário.</response>
    /// <response code="404">Dados pessoais não encontrados.</response>
    [Authorize]
    [HttpPut("update-person")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Verifica se um e-mail já está em uso no sistema AgiVysSystem.
    /// </summary>
    /// <param name="email">O e-mail completo a ser consultado.</param>
    /// <response code="200">Retorna true se o e-mail já existir, false caso contrário.</response>
    [AllowAnonymous]
    [HttpGet("check-email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            return BadRequest(new { message = "Formato de e-mail inválido." });
        }

        // Normaliza o e-mail para bater com o padrão do Identity (Upper case)
        var normalizedEmail = email.Trim().ToUpper();

        // Verifica na tabela de Usuários (Identity)
        var exists = await _userManager.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);

        return Ok(new { exists });
    }

    /// <summary>
    /// Verifica se um CPF já está vinculado a alguma conta no sistema.
    /// </summary>
    /// <param name="document">O CPF a ser consultado (apenas números).</param>
    /// <response code="200">Retorna true se o CPF já existir, false caso contrário.</response>
    [AllowAnonymous]
    [HttpGet("check-cpf/{document}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCpf(string document)
    {
        // Remove pontos e traços caso o front-end envie formatado
        var cleanDocument = new string(document.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(cleanDocument) || cleanDocument.Length != 11)
        {
            return BadRequest(new { message = "CPF inválido." });
        }

        // Verifica na tabela People se já existe o documento
        var exists = await _context.People.AnyAsync(p => p.Document == cleanDocument);

        return Ok(new { exists });
    }

    /// <summary>
    /// Envia um e-mail para recuperação de senha..
    /// </summary>
    /// <param name="model">Informe um e-mail válido para receber a recuperação de senha.</param>
    /// <returns>Retorna uma mensagem de E-mail enviado com sucesso.</returns>
    /// <response code="200">Link de recuperação enviado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) 
            return Ok(new { message = "Se o e-mail existir em nossa base, um link de recuperação será enviado." });

        // Gera o Token de recuperação (Identity gera um token seguro)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // Link para o seu Frontend (Exemplo: React/Angular)
        // O token precisa ser codificado para URL para não quebrar caracteres especiais
        var callbackUrl = $"https://seufrotend.com.br/reset-password?token={Uri.EscapeDataString(token)}&email={user.Email}";

        var resetMessage = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 8px; overflow: hidden;'>
            <div style='background-color: #1a1a1a; padding: 20px; text-align: center;'>
                <h1 style='color: #ffffff; margin: 0; font-size: 24px;'>Bit System</h1>
            </div>
            <div style='padding: 30px; color: #333; line-height: 1.6;'>
                <h2 style='color: #1a1a1a;'>Recuperação de Senha</h2>
                <p>Olá, {user.UserName}!</p>
                <p>Recebemos uma solicitação para redefinir a sua senha no <strong>Bit System</strong>.</p>
                <p>Este link é válido por <strong>2 horas</strong>. Se você não solicitou esta alteração, ignore este e-mail.</p>
                
                <div style='margin: 30px 0; text-align: center;'>
                    <a href='{callbackUrl}' style='background-color: #d9534f; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Redefinir Senha</a>
                </div>

                <hr style='border: 0; border-top: 1px solid #eee;' />
                <p style='font-size: 12px; color: #777;'>Por segurança, nunca compartilhe este link com ninguém.</p>
            </div>
            <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #999;'>
                &copy; {DateTime.Now.Year} Bit System - Todos os direitos reservados.
            </div>
        </div>";

        await _emailService.SendEmailAsync(user.Email!, "Recuperação de Senha - Bit System", resetMessage);

        return Ok(new { message = "Link de recuperação enviado com sucesso." });
    }

    /// <summary>
    /// Altera a senha.
    /// </summary>
    /// <param name="model">Altera a senha do usuário</param>
    /// <returns>Retorna uma mensagem de senha alterada com sucesso</returns>
    /// <response code="200">Senha alterada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) 
            return BadRequest(new { message = "Dados inválidos." });

        // Decodifica o token caso ele venha da URL (evita o erro que tivemos antes)
        string decodedToken = System.Net.WebUtility.UrlDecode(model.Token);

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

        if (result.Succeeded)
        {
            return Ok(new { message = "Senha atualizada com sucesso!" });
        }

        // Em vez de retornar result.Errors direto, retornamos sua mensagem padronizada
        return BadRequest(new { message = "Dados inválidos." });
    }

    // --- MÉTODOS DE GESTÃO DE ENDEREÇOS PESSOAIS ---

    /// <summary>
    /// Lista todos os endereços vinculados ao perfil do usuário autenticado.
    /// </summary>
    /// <response code="200">Retorna a lista de endereços.</response>
    /// <response code="401">Usuário não autenticado.</response>
    [Authorize]
    [HttpGet("my-addresses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// Adiciona um novo endereço ao perfil do usuário autenticado.
    /// </summary>
    /// <param name="dto">Dados do endereço (CEP, Rua, Número, etc).</param>
    /// <response code="201">Endereço cadastrado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Perfil (Person) não encontrado para o usuário.</response>
    [Authorize]
    [HttpPost("my-addresses")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Atualiza um endereço existente do usuário autenticado.
    /// </summary>
    /// <param name="id">ID do endereço.</param>
    /// <param name="dto">Novos dados para o endereço.</param>
    /// <response code="200">Endereço atualizado com sucesso.</response>
    /// <response code="403">Tentativa de alterar endereço de outro usuário.</response>
    /// <response code="404">Endereço não encontrado.</response>
    [Authorize]
    [HttpPut("my-addresses/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Remove um endereço do perfil do usuário autenticado.
    /// </summary>
    /// <param name="id">ID do endereço a ser removido.</param>
    /// <response code="200">Endereço removido com sucesso.</response>
    /// <response code="403">Tentativa de remover endereço de outro usuário.</response>
    /// <response code="404">Endereço não encontrado.</response>
    [Authorize]
    [HttpDelete("my-addresses/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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