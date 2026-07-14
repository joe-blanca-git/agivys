using System;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.Configuration;

namespace AgiVysSystem.Api.Models.User;

[Table("UserSystems")]
public class UserSystem
{
    public int UserId { get; set; }
    public int AppSystemId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("AppSystemId")]
    public virtual AppSystem AppSystem { get; set; } = null!;
}
