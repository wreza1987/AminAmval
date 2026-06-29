using System.ComponentModel.DataAnnotations;
namespace AssetKeeper.Domain.Enums;

public enum AssetOwner
{
    [Display(Name = "بانک مسکن")]
    MaskanBank,

    [Display(Name = "ناواکو")]
    Navaco,

    [Display(Name = "بقیه")]
    Other,

    [Display(Name = "نامعلوم")]
    Unknown
}
