using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.User;

namespace AgiVysSystem.Api.Models.Configuration;

[Table("UserSystem")]
public class UserSystem
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SystemId { get; set; }

    [ForeignKey("UserId")]
    public User.User? User { get; set; }

    [ForeignKey("SystemId")]
    public AppSystem? System { get; set; }
}
