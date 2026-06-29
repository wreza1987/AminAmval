using System.ComponentModel.DataAnnotations;
namespace AssetKeeper.Domain.Enums;

public enum EmployeeAccessLevel
{
    [Display(Name = "کاربر عادی")]
    Normal,         // کاربر عادی

    [Display(Name = "انباردار")]
    WarehouseKeeper, // انباردار

    [Display(Name = "مدیر سامانه")]
    Admin,           // مدیر سامانه

    [Display(Name = "غیرفعال")]
    Disable
}
