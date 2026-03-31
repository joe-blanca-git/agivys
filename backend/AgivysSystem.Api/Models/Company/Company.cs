using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.Configuration;

namespace AgiVysSystem.Api.Models.Company;

[Table("Company")]
public class Company
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(18)]
    public string Cnpj { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public int UserOwnerId { get; set; }
    
    [ForeignKey("UserOwnerId")]
    public virtual AgiVysSystem.Api.Models.User.User? Owner { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}