using Microsoft.AspNetCore.Identity;

namespace AssetKeeper.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? PersonnelCode { get; set; }
    public int? EmployeeId { get; set; }
}