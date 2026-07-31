namespace AgiVysSystem.Api.DTOs.RLS;

public class RemoveRoleRequestDto
{
    public int UserId { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
}
