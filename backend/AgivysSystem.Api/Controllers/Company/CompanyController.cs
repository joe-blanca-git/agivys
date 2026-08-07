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
    /// Cadastro Completo de Empresa (com Endereço)
    /// </summary>
    /// <remarks>
    /// Cadastra simultaneamente uma nova empresa (Company) e seu respectivo endereço (CompanyAddress) dentro de uma única transação no banco de dados.
    /// Em caso de falha em qualquer etapa, a operação inteira é desfeita (Rollback).
    /// </remarks>
    /// <param name="dto">Dados consolidados da empresa e do endereço.</param>
    /// <response code="200">Empresa e endereço cadastrados com sucesso.</response>
    /// <response code="400">Dados inválidos ou CNPJ já existente.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro interno do servidor ao salvar os dados.</response>
    [HttpPost("create-with-address")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// Cadastro Simplificado de Empresa
    /// </summary>
    /// <remarks>
    /// Cria uma nova empresa vinculada ao usuário proprietário especificado. 
    /// O CNPJ é validado para garantir que não haja duplicidade na plataforma.
    /// </remarks>
    /// <param name="dto">Dados básicos da empresa (Nome, CNPJ, Logo).</param>
    /// <response code="200">Empresa cadastrada com sucesso.</response>
    /// <response code="400">Dados inválidos ou CNPJ já cadastrado.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro interno do servidor.</response>
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
    /// Listagem de Empresas por Proprietário
    /// </summary>
    /// <remarks>
    /// Recupera todas as empresas cujo `UserOwnerId` corresponde ao ID fornecido.
    /// É comumente utilizado para preencher o seletor de contextos do Dashboard.
    /// </remarks>
    /// <param name="userId">ID numérico do usuário proprietário.</param>
    /// <response code="200">Lista de empresas recuperada com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet("owner/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByOwner(int userId)
    {
        var companies = await _context.Companies
            .Where(c => c.UserOwnerId == userId)
            .ToListAsync();
            
        return Ok(companies);
    }

    /// <summary>
    /// Atualização de Empresa
    /// </summary>
    /// <remarks>
    /// Atualiza dados cadastrais básicos de uma empresa.
    /// **Regra de Segurança**: Somente o próprio dono (UserOwnerId) da empresa possui permissão para atualizá-la (validação feita cruzando o ID do BD com o do Token).
    /// </remarks>
    /// <param name="id">ID numérico único da empresa.</param>
    /// <param name="dto">Dados atualizados da empresa.</param>
    /// <response code="200">Empresa atualizada com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Acesso negado. O usuário não é o dono da empresa.</response>
    /// <response code="404">Empresa não localizada.</response>
    /// <response code="500">Erro interno do servidor ao atualizar a empresa.</response>
    [HttpPut("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Exclusão de Empresa
    /// </summary>
    /// <remarks>
    /// Remove completamente o registro da empresa. Esta é uma deleção física no banco de dados.
    /// É altamente recomendável garantir que a empresa não possua registros financeiros dependentes antes da deleção.
    /// </remarks>
    /// <param name="id">ID numérico único da empresa.</param>
    /// <response code="200">Empresa removida com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="404">Empresa não localizada.</response>
    /// <response code="500">Erro interno do servidor ao tentar deletar a empresa (ex: violação de chave estrangeira).</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Cadastro de Endereço Empresarial
    /// </summary>
    /// <remarks>
    /// Vincula um novo endereço a uma empresa existente. Uma empresa pode ter múltiplos endereços (ex: Matriz, Filial, Galpão).
    /// **Regra de Segurança**: Apenas o dono da empresa pode adicionar endereços à mesma.
    /// </remarks>
    /// <param name="companyId">ID único numérico da empresa associada.</param>
    /// <param name="dto">Dados do endereço.</param>
    /// <response code="201">Endereço cadastrado e vinculado com sucesso à empresa.</response>
    /// <response code="400">Dados de endereço inválidos ou malformados.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. O usuário não é o dono da empresa.</response>
    /// <response code="404">Empresa não encontrada para realizar o vínculo.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPost("{companyId}/addresses")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Listagem de Endereços Empresariais
    /// </summary>
    /// <remarks>
    /// Recupera a lista completa de endereços atrelados a uma empresa específica.
    /// Somente o dono da empresa pode visualizar seus respectivos endereços.
    /// </remarks>
    /// <param name="companyId">ID único numérico da empresa.</param>
    /// <response code="200">Lista de endereços recuperada com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. O usuário não é o dono da empresa.</response>
    /// <response code="404">A empresa não foi localizada.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet("{companyId}/addresses")]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Atualização de Endereço Empresarial
    /// </summary>
    /// <remarks>
    /// Edita as informações de um endereço de empresa já existente.
    /// **Regra de Segurança**: Esta operação valida cruzadamente se o usuário logado possui a empresa dona do endereço.
    /// </remarks>
    /// <param name="addressId">ID numérico único do endereço a ser modificado.</param>
    /// <param name="dto">Novos dados completos do endereço.</param>
    /// <response code="200">Endereço atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos enviados no payload.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. Endereço pertence a uma empresa de outro usuário.</response>
    /// <response code="404">Endereço não localizado.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPut("addresses/{addressId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Exclusão de Endereço Empresarial
    /// </summary>
    /// <remarks>
    /// Remove definitivamente um endereço vinculado a uma empresa.
    /// Valida previamente se a pessoa que está executando a exclusão é o proprietário da empresa detentora do endereço.
    /// </remarks>
    /// <param name="addressId">ID numérico único do endereço a ser removido.</param>
    /// <response code="200">Endereço removido permanentemente.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada. O usuário não tem acesso a este endereço.</response>
    /// <response code="404">Endereço não localizado.</response>
    /// <response code="500">Erro interno do servidor ao tentar excluir o endereço.</response>
    [HttpDelete("addresses/{addressId}")]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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