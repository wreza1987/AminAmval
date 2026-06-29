using AssetKeeper.Domain.Enums;

namespace AssetKeeper.Domain.Entities;

public class PagePermission
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;      // مثلاً "Assets/Index"
    public string PageTitle { get; set; } = string.Empty;    // مثلاً "لیست اموال"
    public EmployeeAccessLevel AccessLevel { get; set; }
    public bool IsAllowed { get; set; }
}