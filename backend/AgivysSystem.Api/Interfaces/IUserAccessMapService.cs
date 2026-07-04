using AgiVysSystem.Api.DTOs.UserAccessMap;

namespace AgiVysSystem.Api.Interfaces;

public interface IUserAccessMapService
{
    Task AddUserAccessMapAsync(UserAccessMapDto dto);
    Task RemoveUserAccessMapAsync(UserAccessMapDto dto);
    Task<List<UserAccessMapResponseDto>> GetUserAccessMapAsync(int userId);
}
