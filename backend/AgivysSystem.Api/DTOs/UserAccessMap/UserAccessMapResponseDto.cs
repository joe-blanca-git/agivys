namespace AgiVysSystem.Api.DTOs.UserAccessMap;

public class UserAccessMapResponseDto
{
    public int AppSystemId { get; set; }
    public string AppSystemName { get; set; } = string.Empty;
    public List<MenuDto> Menus { get; set; } = new();

    public class MenuDto
    {
        public int MenuId { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public List<SubmenuDto> Submenus { get; set; } = new();
    }

    public class SubmenuDto
    {
        public int SubmenuId { get; set; }
        public string SubmenuName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
    }
}
