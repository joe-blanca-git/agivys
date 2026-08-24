using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.Configuration;
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

    // NULL = conta de plataforma (cliente AGIVYS) ou usuário legado.
    // Não-nulo = usuário final de um AppSystem específico — o par (Email, PrimaryAppSystemId)
    // é o que precisa ser único, não o e-mail isolado (ver índice único em AppDbContext).
    public int? PrimaryAppSystemId { get; set; }

    [ForeignKey("PrimaryAppSystemId")]
    public AppSystem? PrimaryAppSystem { get; set; }

    public virtual ICollection<UserSystem> UserSystems { get; set; } = new List<UserSystem>();
}