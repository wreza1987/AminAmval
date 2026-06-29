using System.ComponentModel.DataAnnotations;
using AssetKeeper.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetKeeper.Domain.Entities;

public class Asset
{
    public int Id { get; set; }

    [Required(ErrorMessage = "کد اموال الزامی است")]
    [Display(Name = "کد اموال")]
    public string AssetCode { get; set; } = string.Empty;

    [Display(Name = "کد اموال قبلی")]
    public string? OldAssetCode { get; set; }

    [Required(ErrorMessage = "نام اموال الزامی است")]
    [Display(Name = "نام اموال")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "شماره سریال")]
    public string? SerialNumber { get; set; }

    [Display(Name = "توضیحات")]
    public string? Description { get; set; }

    [Display(Name = "مسیر تصویر")]
    public string? ImagePath { get; set; }

    [NotMapped]
    [Display(Name = "تصویر کالا")]
    public IFormFile? ImageFile { get; set; }   // برای آپلود

    [Required(ErrorMessage = "دسته‌بندی الزامی است")]
    [Display(Name = "دسته‌بندی")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "برند الزامی است")]
    [Display(Name = "برند")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "مالک الزامی است")]
    [Display(Name = "مالک")]
    public AssetOwner Owner { get; set; } = AssetOwner.Navaco;

    public AssetStatus Status { get; set; } = AssetStatus.InStock;

    [Display(Name = "تاریخ تغییر وضعیت")]
    public DateTime? StatusChangeDate { get; set; }

    [Display(Name = "توضیحات وضعیت")]
    public string? StatusNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation Properties
    public Category Category { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
    public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();
    public ICollection<AssetHistory> History { get; set; } = new List<AssetHistory>();
}