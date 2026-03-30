namespace AgiVysSystem.Api.Dtos;

public class CreateMenuDto
{
    public int SystemId { get; set; }
    public MenuEntry Menu { get; set; } = null!;

    public class MenuEntry
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public List<SubmenuEntry>? Submenu { get; set; }
    }

    public class SubmenuEntry
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Route { get; set; }
    }
}

public class UpdateMenuDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Route { get; set; }
    public string? Icon { get; set; }
}