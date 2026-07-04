using System.ComponentModel.DataAnnotations;

namespace AgiVysSystem.Api.DTOs.UserAccessMap;

public class UserAccessMapDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int MenuId { get; set; }

    [Required]
    public int AppSystemId { get; set; }
}
