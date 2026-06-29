using System.ComponentModel.DataAnnotations;

namespace AssetKeeper.Dtos;

public class AssetAssignmentCreateDto
{
    [Required(ErrorMessage = "اموال الزامی است")]
    [Display(Name = "اموال")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "پرسنل الزامی است")]
    [Display(Name = "پرسنل")]
    public int EmployeeId { get; set; }

    [Display(Name = "تاریخ تخصیص")]
    public DateTime AssignmentDate { get; set; }

    [Display(Name = "توضیحات")]
    public string? Notes { get; set; }
}