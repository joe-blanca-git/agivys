using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.People;

namespace AgiVysSystem.Api.Models.User;

[Table("Users")]
public class User : IdentityUser<int>
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public int? PersonId { get; set; }
    [ForeignKey("PersonId")]
    public Person? Person { get; set; }
}