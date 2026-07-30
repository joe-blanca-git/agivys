using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgiVysSystem.Api.Models.Configuration;

public class Integration
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AppSystemId { get; set; }

    [ForeignKey("AppSystemId")]
    public AppSystem AppSystem { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public ICollection<IntegrationParameter> Parameters { get; set; } = new List<IntegrationParameter>();
}
