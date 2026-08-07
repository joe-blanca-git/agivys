using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.Models.Configuration;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using AgiVysSystem.Api.Dtos;

namespace AgiVysSystem.Api.Controllers.Configuration;

/// <summary>
/// Controlador responsável por gerenciar o ecossistema AgiVysSystem: Sistemas, Menus, Submenus e Planos.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/systems")]
[ApiController]
[Authorize(Roles = "Admin, Dev")]
public class AppSystemController : ControllerBase
{
    private readonly AppDbContext _context;

    public AppSystemController(AppDbContext context)
    {
        _context = context;
    }

    #region AppSystem (Sistemas)

    /// <summary>
    /// Cadastro de Sistema (AppSystem)
    /// </summary>
    /// <remarks>
    /// Adiciona um novo sistema raiz no ecossistema AgiVys. Nomes de sistemas devem ser únicos.
    /// Requer a role de Administrador ou Desenvolvedor (Admin, Dev).
    /// </remarks>
    /// <param name="dto">Dados do sistema (Nome, Descrição).</param>
    /// <response code="200">Sistema criado com sucesso.</response>
    /// <response code="400">Dados inválidos ou nome já existente.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateAppSystemDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var exists = await _context.AppSystems.AnyAsync(s => s.Name.ToLower() == dto.Name.ToLower());
            if (exists) return BadRequest(new { message = "Já existe um sistema cadastrado com este nome." });

            var systemToSave = new AppSystem
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.AppSystems.Add(systemToSave);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sistema criado com sucesso!", id = systemToSave.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao salvar o sistema.", details = ex.Message });
        }
    }

    /// <summary>
    /// Listagem Geral de Sistemas
    /// </summary>
    /// <remarks>
    /// Recupera a lista completa de todos os sistemas (AppSystems) registrados na plataforma.
    /// </remarks>
    /// <response code="200">Lista de sistemas recuperada com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="500">Erro interno no servidor.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var systems = await _context.AppSystems.AsNoTracking().ToListAsync();
            return Ok(systems);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao listar sistemas.", details = ex.Message });
        }
    }

    /// <summary>
    /// Atualização de Sistema
    /// </summary>
    /// <remarks>
    /// Atualiza nome, descrição e parâmetros visuais (cores, domínio) de um sistema existente.
    /// Valida se o novo nome não conflita com outro sistema.
    /// </remarks>
    /// <param name="id">ID numérico único do sistema.</param>
    /// <param name="dto">Novos dados do sistema.</param>
    /// <response code="200">Sistema atualizado com sucesso.</response>
    /// <response code="400">ID da rota incompatível com payload ou nome duplicado.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="404">Sistema não localizado.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppSystemDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "ID da rota não confere com o ID do payload." });

        try
        {
            var system = await _context.AppSystems.FindAsync(id);
            if (system == null) return NotFound(new { message = "Sistema não encontrado." });

            var exists = await _context.AppSystems.AnyAsync(s => s.Name.ToLower() == dto.Name.ToLower() && s.Id != id);
            if (exists) return BadRequest(new { message = "Já existe outro sistema cadastrado com este nome." });

            system.Name = dto.Name;
            system.Description = dto.Description;
            system.BackgroundColor = dto.BackgroundColor;
            system.TextColor = dto.TextColor;
            system.Domain = dto.Domain;

            _context.AppSystems.Update(system);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sistema atualizado com sucesso!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao atualizar o sistema.", details = ex.Message });
        }
    }

    /// <summary>
    /// Exclusão de Sistema
    /// </summary>
    /// <remarks>
    /// Remove um sistema e aciona exclusão em cascata das suas entidades filhas (Menus, Planos, etc) dependendo da configuração do banco de dados.
    /// </remarks>
    /// <param name="id">ID numérico do sistema.</param>
    /// <response code="200">Sistema removido com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="404">Sistema não localizado.</response>
    /// <response code="500">Erro interno (ex: falha de deleção em cascata).</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var system = await _context.AppSystems.FindAsync(id);
            if (system == null) return NotFound(new { message = "Sistema não encontrado." });

            _context.AppSystems.Remove(system);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sistema removido com sucesso." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Erro ao remover sistema. Verifique dependências." });
        }
    }

    #endregion

    #region Menus e Submenus

    /// <summary>
    /// Cadastro de Hierarquia de Menus
    /// </summary>
    /// <remarks>
    /// Cadastra um menu (Agrupador) e seus respectivos submenus (Funcionalidades) de forma atômica utilizando transação no banco.
    /// </remarks>
    /// <param name="dto">Payload contendo os dados do Menu pai e array de Submenus.</param>
    /// <response code="200">Hierarquia cadastrada com sucesso.</response>
    /// <response code="400">Payload inválido ou Sistema Pai (AppSystem) não localizado.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="500">Erro na transação de banco de dados.</response>
    [HttpPost("menus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateMenuHierarchy([FromBody] CreateMenuDto dto)
    {
        var systemExists = await _context.AppSystems.AnyAsync(s => s.Id == dto.SystemId);
        if (!systemExists) return BadRequest(new { message = "Sistema pai não encontrado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var newMenu = new Menu
            {
                Name = dto.Menu.Name,
                Icon = dto.Menu.Icon,
                Route = dto.Menu.Route,
                AppSystemId = dto.SystemId
            };

            _context.Menus.Add(newMenu);
            await _context.SaveChangesAsync();

            if (dto.Menu.Submenu != null && dto.Menu.Submenu.Any())
            {
                foreach (var sub in dto.Menu.Submenu)
                {
                    _context.Submenus.Add(new Submenu
                    {
                        Name = sub.Name,
                        Description = sub.Description,
                        Route = sub.Route ?? "",
                        MenuId = newMenu.Id
                    });
                }
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return Ok(new { message = "Menu e submenus cadastrados com sucesso!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Falha na transação", details = ex.InnerException?.Message ?? ex.Message });
        }
    }

    /// <summary>
    /// Listagem de Menus por Sistema
    /// </summary>
    /// <remarks>
    /// Recupera a árvore completa (Menus e Submenus) atrelada a um sistema específico.
    /// A resposta é estruturada hierarquicamente para facilitar a montagem da UI do front-end.
    /// </remarks>
    /// <param name="systemId">ID do sistema a ser consultado.</param>
    /// <response code="200">Árvore de menus recuperada.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet("{systemId}/menus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemMenus(int systemId)
    {
        try
        {
            var menus = await _context.Menus
                .Include(m => m.Submenus)
                .Where(m => m.AppSystemId == systemId)
                .OrderBy(m => m.Name)
                .Select(m => new {
                    m.Id,
                    m.Name,
                    m.Icon,
                    Submenus = m.Submenus.Select(s => new {
                        s.Id,
                        s.Name,
                        s.Description,
                        s.Route
                    }).ToList()
                })
                .ToListAsync();

            return Ok(menus);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar menus.", details = ex.Message });
        }
    }

    /// <summary>
    /// Atualização de Menu Pai
    /// </summary>
    /// <remarks>
    /// Edita apenas os dados de um Menu pai (Nome e Ícone). Não altera submenus diretamente por este endpoint.
    /// </remarks>
    /// <param name="id">ID do menu a ser atualizado.</param>
    /// <param name="dto">Novos dados do menu.</param>
    /// <response code="200">Menu atualizado.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">Menu não encontrado.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpPut("menus/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMenu(int id, [FromBody] UpdateMenuDto dto)
    {
        var menu = await _context.Menus.FindAsync(id);
        if (menu == null) return NotFound(new { message = "Menu não encontrado." });

        try
        {
            menu.Name = dto.Name;
            menu.Icon = dto.Icon;
            // Nota: Descrição e Rota pertencem aos Submenus na nova estrutura.
            
            _context.Menus.Update(menu);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Menu atualizado com sucesso!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao atualizar.", details = ex.Message });
        }
    }

    /// <summary>
    /// Exclusão de Menu (em cascata)
    /// </summary>
    /// <remarks>
    /// Remove o Menu Pai e, consequentemente, destrói todos os seus Submenus atrelados no banco de dados.
    /// </remarks>
    /// <param name="id">ID do menu a ser removido.</param>
    /// <response code="200">Menu e filhos removidos.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">Menu não encontrado.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpDelete("menus/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMenu(int id)
    {
        var menu = await _context.Menus.FindAsync(id);
        if (menu == null) return NotFound();

        _context.Menus.Remove(menu);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Menu e submenus removidos." });
    }

    #endregion

    #region Plans (Planos)

    /// <summary>
    /// Criação de Plano de Assinatura
    /// </summary>
    /// <remarks>
    /// Cria um novo plano comercial para um Sistema, vinculando quais Menus e Submenus os assinantes deste plano terão acesso.
    /// Oferece controle granular de permissões (Access Control List baseado em plano).
    /// </remarks>
    /// <param name="dto">Payload com os dados do plano (Nome, Preço, Trial) e as permissões de acesso.</param>
    /// <response code="200">Plano criado e permissões vinculadas com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada (Role insuficiente).</response>
    /// <response code="500">Erro na transação ao vincular permissões.</response>
    [HttpPost("plans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto) 
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try 
        {
            var menuIds = dto.Permissions.Select(p => p.MenuId).ToList();
            var submenuIds = dto.Permissions.SelectMany(p => p.SubmenuIds).ToList();

            var menus = await _context.Menus.Where(m => menuIds.Contains(m.Id)).ToListAsync();
            var submenus = await _context.Submenus.Where(s => submenuIds.Contains(s.Id)).ToListAsync();

            var newPlan = new Plan 
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                MaxEmployees = dto.MaxEmployees,
                TrialDays = dto.TrialDays,
                AppSystemId = dto.AppSystemId,
                AllowedMenus = menus,
                AllowedSubmenus = submenus
            };

            _context.Plans.Add(newPlan);
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            return Ok(new { message = "Plano criado com sucesso!", planId = newPlan.Id });
        } 
        catch (Exception ex) 
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Erro ao criar plano", details = ex.InnerException?.Message ?? ex.Message });
        }
    }

    /// <summary>
    /// Listagem de Planos por Sistema
    /// </summary>
    /// <remarks>
    /// Recupera a lista de planos de um determinado sistema, expandindo (Include) detalhadamente as permissões (Menus e Submenus) liberadas por cada plano.
    /// </remarks>
    /// <param name="systemId">ID do sistema a ser consultado.</param>
    /// <response code="200">Planos e permissões recuperados com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">Nenhum plano cadastrado para o sistema informado.</response>
    /// <response code="500">Erro interno do servidor.</response>
    [HttpGet("{systemId}/plans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlansBySystem(int systemId)
    {
        try
        {
            // 1. Buscamos os dados do banco primeiro (sem o Select complexo)
            var plansData = await _context.Plans
                .Include(p => p.AllowedMenus)
                .Include(p => p.AllowedSubmenus)
                .Where(p => p.AppSystemId == systemId)
                .ToListAsync();

            if (!plansData.Any()) 
                return NotFound(new { message = "Nenhum plano encontrado para este sistema." });

            // 2. Agora formatamos o resultado na memória (C# puro)
            var response = plansData.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.MaxEmployees,
                p.TrialDays,
                Permissions = p.AllowedMenus
                    .OrderBy(m => m.Name)
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.Icon,
                        // Aqui o C# filtra os submenus que pertencem a este menu
                        Submenus = p.AllowedSubmenus
                                    .Where(s => s.MenuId == m.Id)
                                    .OrderBy(s => s.Name)
                                    .Select(s => new
                                    {
                                        s.Id,
                                        s.Name,
                                        s.Route
                                    }).ToList()
                    }).ToList()
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar planos.", details = ex.Message });
        }
    }

    /// <summary>
    /// Atualização de Plano e Permissões
    /// </summary>
    /// <remarks>
    /// Modifica os dados básicos do plano (Preço, Trial, etc) e recria os vínculos de acesso aos Menus e Submenus associados.
    /// Utiliza transação para garantir que a troca de permissões seja atômica.
    /// </remarks>
    /// <param name="id">ID do plano a ser atualizado.</param>
    /// <param name="dto">Payload completo contendo a nova configuração do plano.</param>
    /// <response code="200">Plano e acessos atualizados com sucesso.</response>
    /// <response code="400">Payload com dados malformados.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">Plano não localizado na base de dados.</response>
    /// <response code="500">Falha durante o commit da transação de atualização.</response>
    [HttpPut("plans/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] CreatePlanDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Buscamos o plano incluindo as coleções de relacionamento
        var plan = await _context.Plans
            .Include(p => p.AllowedMenus)
            .Include(p => p.AllowedSubmenus)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return NotFound(new { message = "Plano não encontrado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Atualiza dados básicos
            plan.Name = dto.Name;
            plan.Description = dto.Description;
            plan.Price = dto.Price;
            plan.MaxEmployees = dto.MaxEmployees;
            plan.TrialDays = dto.TrialDays;

            // 2. Extrai novos IDs do DTO
            var newMenuIds = dto.Permissions.Select(p => p.MenuId).ToList();
            var newSubmenuIds = dto.Permissions.SelectMany(p => p.SubmenuIds).ToList();

            // 3. Busca os novos objetos no banco
            var newMenus = await _context.Menus.Where(m => newMenuIds.Contains(m.Id)).ToListAsync();
            var newSubmenus = await _context.Submenus.Where(s => newSubmenuIds.Contains(s.Id)).ToListAsync();

            // 4. Sincroniza as coleções (O EF cuida das tabelas associativas PlanMenus e PlanSubmenus)
            plan.AllowedMenus = newMenus;
            plan.AllowedSubmenus = newSubmenus;

            _context.Plans.Update(plan);
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            return Ok(new { message = "Plano e permissões atualizados com sucesso!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Erro ao atualizar plano", details = ex.Message });
        }
    }

    /// <summary>
    /// Exclusão de Plano
    /// </summary>
    /// <remarks>
    /// Deleta fisicamente um plano do banco de dados e remove suas associações com Menus e Submenus.
    /// *Nota*: Caso existam empresas atreladas a este plano, a exclusão pode falhar por restrição de chave estrangeira.
    /// </remarks>
    /// <param name="id">ID do plano a ser excluído.</param>
    /// <response code="200">Plano e vínculos removidos permanentemente.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Permissão negada.</response>
    /// <response code="404">Plano não localizado.</response>
    /// <response code="500">Erro interno (provável violação de restrição do banco de dados).</response>
    [HttpDelete("plans/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePlan(int id)
    {
        try
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null) return NotFound(new { message = "Plano não encontrado." });

            _context.Plans.Remove(plan);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Plano removido com sucesso!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao excluir plano.", details = ex.Message });
        }
    }

    #endregion
}