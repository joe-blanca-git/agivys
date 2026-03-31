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
    /// Cadastra um novo sistema.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// Lista todos os sistemas cadastrados no ecossistema.
    /// </summary>
    /// <response code="200">Retorna a lista de sistemas.</response>
    /// <response code="500">Erro interno no servidor.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    /// Exclui um sistema e seus vínculos em cascata.
    /// </summary>
    [HttpDelete("{id}")]
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
    /// Cadastra um menu e seus respectivos submenus de forma hierárquica.
    /// </summary>
    /// <remarks>
    /// Permite criar o agrupador (Menu) e suas funcionalidades filhas (Submenus) em uma única transação.
    /// </remarks>
    /// <response code="200">Hierarquia criada com sucesso.</response>
    /// <response code="400">Dados inválidos ou IDs inexistentes.</response>
    [HttpPost("menus")]
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
    /// Retorna a árvore de menus e submenus de um sistema específico.
    /// </summary>
    [HttpGet("{systemId}/menus")]
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
    /// Atualiza os dados de um Menu (Pai).
    /// </summary>
    [HttpPut("menus/{id}")]
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
    /// Remove um menu (e todos os seus submenus em cascata).
    /// </summary>
    [HttpDelete("menus/{id}")]
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
    /// Cria um plano vinculando-o a um sistema e selecionando Menus e Submenus específicos.
    /// </summary>
    /// <remarks>
    /// Permite granularidade total: você pode liberar um Menu e apenas alguns de seus Submenus.
    /// </remarks>
    /// <response code="200">Plano criado e permissões vinculadas.</response>
    [HttpPost("plans")]
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
    /// Lista todos os planos de um sistema, detalhando os menus e submenus liberados.
    /// </summary>
    /// <param name="systemId">ID do sistema para filtrar os planos.</param>
    [HttpGet("{systemId}/plans")]
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
    /// Atualiza um plano existente e suas permissões de menus/submenus.
    /// </summary>
    /// <param name="id">ID do plano a ser editado.</param>
    /// <param name="dto">Novos dados do plano e lista de permissões.</param>
    [HttpPut("plans/{id}")]
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
    /// Remove um plano e todos os seus vínculos de permissão.
    /// </summary>
    /// <param name="id">ID do plano a ser removido.</param>
    [HttpDelete("plans/{id}")]
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