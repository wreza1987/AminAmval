using System.ComponentModel.DataAnnotations;
namespace AssetKeeper.Domain.Enums;

public enum ChangeType
{
    [Display(Name = "کد اموال")]
    AssetCodeChanged,

    [Display(Name = "نام اموال")]
    NameChanged,

    [Display(Name = "شماره سریال")]
    SerialNumberChanged,

    [Display(Name = "وضعیت")]
    StatusChanged,

    [Display(Name = "مالک")]
    OwnerChanged,

    [Display(Name = "دسته‌بندی")]
    CategoryChanged,

    [Display(Name = "برند")]
    BrandChanged,

    [Display(Name = "تحویل به پرسنل")]
    AssignedToEmployee,

    [Display(Name = "عودت از پرسنل")]
    ReturnedFromEmployee,

    [Display(Name = "یادداشت جدید")]
    NoteAdded,

    [Display(Name = "موارد دیگر")]
    Other
}