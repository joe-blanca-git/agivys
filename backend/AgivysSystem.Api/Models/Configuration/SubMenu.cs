namespace AgiVysSystem.Api.Models.Configuration;

public class Submenu {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Route { get; set; } = string.Empty;
    
    public int MenuId { get; set; }
    public Menu? Menu { get; set; }
}