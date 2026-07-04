using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs.UserAccessMap;
using AgiVysSystem.Api.Interfaces;
using AgiVysSystem.Api.Models.User;
using Microsoft.EntityFrameworkCore;

namespace AgiVysSystem.Api.Services;

public class UserAccessMapService : IUserAccessMapService
{
    private readonly AppDbContext _context;

    public UserAccessMapService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddUserAccessMapAsync(UserAccessMapDto dto)
    {
        // Verifica se o usuário, menu e sistema existem
        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists) throw new Exception("Usuário não encontrado.");

        var menuExists = await _context.Menus.AnyAsync(m => m.Id == dto.MenuId && m.AppSystemId == dto.AppSystemId);
        if (!menuExists) throw new Exception("Menu ou Sistema inválidos.");

        // Verifica se já existe um vínculo
        var exists = await _context.UserAccessMaps
            .AnyAsync(uam => uam.UserId == dto.UserId && uam.MenuId == dto.MenuId && uam.AppSystemId == dto.AppSystemId);

        if (exists) throw new Exception("O usuário já possui acesso a este menu.");

        var accessMap = new UserAccessMap
        {
            UserId = dto.UserId,
            MenuId = dto.MenuId,
            AppSystemId = dto.AppSystemId
        };

        _context.UserAccessMaps.Add(accessMap);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserAccessMapAsync(UserAccessMapDto dto)
    {
        var accessMap = await _context.UserAccessMaps
            .FirstOrDefaultAsync(uam => uam.UserId == dto.UserId && uam.MenuId == dto.MenuId && uam.AppSystemId == dto.AppSystemId);

        if (accessMap == null) throw new Exception("Vínculo de acesso não encontrado.");

        _context.UserAccessMaps.Remove(accessMap);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserAccessMapResponseDto>> GetUserAccessMapAsync(int userId)
    {
        // Obtém os mapas de acesso do usuário, incluindo Menu e AppSystem, e os submenus dos menus atrelados
        var accessMaps = await _context.UserAccessMaps
            .Include(uam => uam.AppSystem)
            .Include(uam => uam.Menu)
                .ThenInclude(m => m.Submenus)
            .Where(uam => uam.UserId == userId)
            .ToListAsync();

        var response = accessMaps
            .GroupBy(uam => uam.AppSystemId)
            .Select(group => new UserAccessMapResponseDto
            {
                AppSystemId = group.Key,
                AppSystemName = group.First().AppSystem!.Name,
                Menus = group.Select(uam => new UserAccessMapResponseDto.MenuDto
                {
                    MenuId = uam.MenuId,
                    MenuName = uam.Menu!.Name,
                    Icon = uam.Menu.Icon,
                    Submenus = uam.Menu.Submenus.Select(s => new UserAccessMapResponseDto.SubmenuDto
                    {
                        SubmenuId = s.Id,
                        SubmenuName = s.Name,
                        Route = s.Route
                    }).ToList()
                }).ToList()
            }).ToList();

        return response;
    }
}
