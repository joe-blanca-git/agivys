using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.Configuration;

namespace AgiVysSystem.Api.Models.User;

[Table("UserAccessMaps")]
public class UserAccessMap
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int MenuId { get; set; }

    [Required]
    public int AppSystemId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("MenuId")]
    public Menu? Menu { get; set; }

    [ForeignKey("AppSystemId")]
    public AppSystem? AppSystem { get; set; }
}
