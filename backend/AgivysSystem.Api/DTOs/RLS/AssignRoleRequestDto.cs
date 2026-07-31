namespace AgiVysSystem.Api.DTOs.RLS;

public class AssignRoleRequestDto
{
    public int UserId { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
}
