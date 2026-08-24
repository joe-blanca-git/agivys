using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgiVysSystem.Api.Models.Configuration;

[Table("AppSystem")]
public class AppSystem {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? BackgroundColor { get; set; }

    [MaxLength(20)]
    public string? TextColor { get; set; }

    [MaxLength(100)]
    public string? Domain { get; set; }

    // Dono do sistema (cliente AGIVYS que criou). NULL = sistema legado/criado por staff.
    public int? OwnerUserId { get; set; }

    [ForeignKey("OwnerUserId")]
    public AgiVysSystem.Api.Models.User.User? OwnerUser { get; set; }

    public int? CompanyId { get; set; }

    [ForeignKey("CompanyId")]
    public AgiVysSystem.Api.Models.Company.Company? Company { get; set; }
}