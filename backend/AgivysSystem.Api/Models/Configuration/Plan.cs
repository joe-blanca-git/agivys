using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgiVysSystem.Api.Models.Configuration;

[Table("Plan")]
public class Plan {
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public int MaxEmployees { get; set; }
    public int TrialDays { get; set; }
    public int AppSystemId { get; set; }
    
    public AppSystem? AppSystem { get; set; }
    public ICollection<Menu> AllowedMenus { get; set; } = new List<Menu>();
    public ICollection<Submenu> AllowedSubmenus { get; set; } = new List<Submenu>();
}