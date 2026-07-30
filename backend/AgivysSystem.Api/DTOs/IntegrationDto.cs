using System.ComponentModel.DataAnnotations;

namespace AgiVysSystem.Api.DTOs;

public class IntegrationParameterDto
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}

public class IntegrationResponseDto
{
    public int Id { get; set; }
    public int AppSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<IntegrationParameterDto> Parameters { get; set; } = new();
}

public class CreateIntegrationDto
{
    [Required]
    public int AppSystemId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public List<IntegrationParameterDto> Parameters { get; set; } = new();
}

public class UpdateIntegrationDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public List<IntegrationParameterDto> Parameters { get; set; } = new();
}
