using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgiVysSystem.Api.Models.Configuration;

public class IntegrationParameter
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IntegrationId { get; set; }

    [ForeignKey("IntegrationId")]
    public Integration Integration { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}
