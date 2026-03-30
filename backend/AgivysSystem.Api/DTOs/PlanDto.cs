namespace AgiVysSystem.Api.Dtos;

public class CreatePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MaxEmployees { get; set; }
    public int TrialDays { get; set; }
    public int AppSystemId { get; set; }
    
    public List<MenuSelectionDto> Permissions { get; set; } = new();
}

public class MenuSelectionDto {
    public int MenuId { get; set; }
    public List<int> SubmenuIds { get; set; } = new();
}
