using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using AgiVysSystem.Api.Models.Company; 
using AgiVysSystem.Api.Models.Companies;
using System.Security.Claims;

namespace AgiVysSystem.Api.Controllers.Company;

[Route("api/v1/companies")]
[ApiController]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly AppDbContext _context;

    public CompanyController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cadastra uma nova empresa e seu endereço em uma única operação.
    /// </summary>
    [HttpPost("create-with-address")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateWithAddress([FromBody] CreateCompanyWithAddressDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Iniciamos uma transação para garantir integridade
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Validar CNPJ Duplicado
            var cnpjExists = await _context.Companies.AnyAsync(c => c.Cnpj == dto.Cnpj);
            if (cnpjExists) return BadRequest(new { message = "Este CNPJ já está cadastrado." });

            // 2. Criar a Empresa
            var company = new AgiVysSystem.Api.Models.Company.Company
            {
                Name = dto.Name,
                Cnpj = dto.Cnpj,
                LogoUrl = dto.LogoUrl,
                UserOwnerId = dto.UserOwnerId,
                CreatedAt = DateTime.Now
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync(); // Aqui o ID da empresa é gerado

            // 3. Criar o Endereço vinculado ao ID gerado acima
            var address = new CompanyAddress
            {
                CompanyId = company.Id,
                Description = dto.Description,
                ZipCode = dto.ZipCode,
                Street = dto.Street,
                Number = dto.Number,
                Complement = dto.Complement,
                Neighborhood = dto.Neighborhood,
                City = dto.City,
                State = dto.State
            };

            _context.CompanyAddresses.Add(address);
            await _context.SaveChangesAsync();

            // 4. Se tudo deu certo, comita as alterações no banco
            await transaction.CommitAsync();

            return Ok(new 
            { 
                message = "Empresa e endereço cadastrados com sucesso!", 
                companyId = company.Id,
                addressId = address.Id
            });
        }
        catch (Exception ex)
        {
            // Em caso de erro, desfaz tudo o que foi feito nesta chamada
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "Erro ao processar cadastro completo.", details = ex.Message });
        }
    }

    /// <summary>
    /// Cadastra uma nova empresa vinculada a um usuário proprietário.
    /// </summary>
    /// <response code="200">Empresa cadastrada com sucesso!</response>
    /// <response code="400">Este CNPJ já está cadastrado.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro ao cadastrar empresa.</response>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try 
        {
            var cnpjExists = await _context.Companies.AnyAsync(c => c.Cnpj == dto.Cnpj);
            if (cnpjExists) return BadRequest(new { message = "Este CNPJ já está cadastrado." });

            var company = new AgiVysSystem.Api.Models.Company.Company
            {
                Name = dto.Name,
                Cnpj = dto.Cnpj,
                LogoUrl = dto.LogoUrl,
                UserOwnerId = dto.UserOwnerId
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Empresa cadastrada com sucesso!", id = company.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao cadastrar empresa.", details = ex.Message });
        }
    }

    /// <summary>
    /// Lista todas as empresas de um determinado proprietário.
    /// </summary>
    /// <response code="200">Retornas a Empresas do usuário</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro ao obter empresas.</response>
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("owner/{userId}")]
    public async Task<IActionResult> GetByOwner(int userId)
    {
        var companies = await _context.Companies
            .Where(c => c.UserOwnerId == userId)
            .ToListAsync();
            
        return Ok(companies);
    }

    /// <summary>
    /// Atualiza dados básicos da empresa.
    /// </summary>
    /// <response code="200">Empresa atualzizada com sucesso!</response>
    /// <response code="404">Empresa não encontrada.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro ao atualizar empresa.</response>
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPut("{id}")]
    [Produces("application/json")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyDto dto)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return NotFound(new { message = "Empresa não encontrada." });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (company.UserOwnerId != userId)
            return StatusCode(403, new { message = "Acesso negado. Você não é o proprietário desta empresa." });

        try 
        {
            company.Name = dto.Name;
            company.LogoUrl = dto.LogoUrl;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Dados da empresa atualizados." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao atualizar empresa.", details = ex.Message });
        }
    }

    /// <summary>
    /// Remove o registro da empresa.
    /// </summary>
    /// <response code="200">Empresa removida com sucesso!</response>
    /// <response code="404">Empresa não encontrada.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro ao remover empresa.</response>
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return NotFound(new { message = "Empresa não encontrada." });

        try 
        {
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Empresa removida com sucesso." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao remover empresa.", details = ex.Message });
        }
    }

    /// <summary>
    /// Adiciona um novo endereço para uma empresa específica.
    /// </summary>
    /// <response code="201">Endereço cadastrado com sucesso.</response>
    /// <response code="403">Você não tem permissão para esta empresa.</response>
    [HttpPost("{companyId}/addresses")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddAddress(int companyId, [FromBody] CompanyAddressDto dto)
    {
        var company = await _context.Companies.FindAsync(companyId);
        if (company == null) return NotFound(new { message = "Empresa não encontrada." });

        // Validação de Dono
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (company.UserOwnerId != userId) return Forbid();

        var address = new CompanyAddress
        {
            CompanyId = companyId,
            Description = dto.Description,
            ZipCode = dto.ZipCode,
            Street = dto.Street,
            Number = dto.Number,
            Complement = dto.Complement,
            Neighborhood = dto.Neighborhood,
            City = dto.City,
            State = dto.State
        };

        _context.CompanyAddresses.Add(address);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { message = "Endereço adicionado.", id = address.Id });
    }

    /// <summary>
    /// Lista todos os endereços de uma empresa.
    /// </summary>
    /// <response code="200"></response>
    /// <response code="404">Nenhum endereço encontrado.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro ao remover empresa.</response>
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("{companyId}/addresses")]
    public async Task<IActionResult> GetAddresses(int companyId)
    {
        var company = await _context.Companies.FindAsync(companyId);
        if (company == null) return NotFound();

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (company.UserOwnerId != userId) return Forbid();

        var addresses = await _context.CompanyAddresses
            .Where(a => a.CompanyId == companyId)
            .ToListAsync();

        return Ok(addresses);
    }

    /// <summary>
    /// Atualiza um endereço existente (Apenas o proprietário).
    /// </summary>
    /// <response code="200">Endereço atualizado com sucesso!</response>
    /// <response code="404">Endereço não encontrada.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="500">Erro ao atualizar endereço.</response>
    [HttpPut("addresses/{addressId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddress(int addressId, [FromBody] CompanyAddressDto dto)
    {
        // Buscamos o endereço incluindo a empresa para checar o dono
        var address = await _context.CompanyAddresses
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == addressId);

        if (address == null) return NotFound(new { message = "Endereço não encontrado." });

        // Validação de segurança baseada no Token
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (address.Company.UserOwnerId != userId) 
            return StatusCode(403, new { message = "Permissão negada." });

        address.Description = dto.Description;
        address.ZipCode = dto.ZipCode;
        address.Street = dto.Street;
        address.Number = dto.Number;
        address.Complement = dto.Complement;
        address.Neighborhood = dto.Neighborhood;
        address.City = dto.City;
        address.State = dto.State;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Endereço atualizado com sucesso." });
    }

    /// <summary>
    /// Remove um endereço específico.
    /// </summary>
    /// <response code="200">Endereço removida com sucesso!</response>
    /// <response code="404">Endereço não encontrada.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro ao remover endereço.</response>
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("addresses/{addressId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        var address = await _context.CompanyAddresses
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == addressId);

        if (address == null) return NotFound();

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (address.Company.UserOwnerId != userId) return Forbid();

        _context.CompanyAddresses.Remove(address);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Endereço removido." });
    }
}