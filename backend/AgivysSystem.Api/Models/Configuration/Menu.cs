namespace AgiVysSystem.Api.Models.Configuration;

public class Menu {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int AppSystemId { get; set; }
    public AppSystem? AppSystem { get; set; }
    
    // Um Menu tem vários Submenus
    public ICollection<Submenu> Submenus { get; set; } = new List<Submenu>();
}