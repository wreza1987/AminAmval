using AssetKeeper.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AssetKeeper.Domain.Entities;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "کد پرسنلی")]
    public string PersonnelCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "نام")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "نام خانوادگی")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "کد ملی")]
    public string NationalCode { get; set; } = string.Empty;

    [Display(Name = "معاونت")]
    public string? VicePresidency { get; set; }  

    [Display(Name = "دپارتمان/واحد")]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "تاریخ شروع به کار")]
    public DateTime StartDate { get; set; }

    public bool IsActive { get; set; } = true;
    public EmployeeAccessLevel AccessLevel { get; set; } = EmployeeAccessLevel.Normal;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "تاریخچه تغییرات")]
    public string? Description { get; set; }

    public string? IdentityUserId { get; set; }   // لینک به کاربر Identity

    public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();
}