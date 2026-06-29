using System.ComponentModel.DataAnnotations;
namespace AssetKeeper.Domain.Enums;


public enum AssetStatus
{
    [Display(Name = "در انبار")]
    InStock,        // در انبار

    [Display(Name = "تحویل پرسنل")]
    Assigned,       // تحویل پرسنل

    [Display(Name = "در تعمیرات")]
    UnderRepair,    // در تعمیرات

    [Display(Name = "اسقاط")]
    Scrapped,       // اسقاط

    [Display(Name = "مفقود")]
    Lost,            // مفقود
    
    [Display(Name = "فروش رفته")]
    Sell            // فروش رفته
}

