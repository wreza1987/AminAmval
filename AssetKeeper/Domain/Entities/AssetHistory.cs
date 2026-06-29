using AssetKeeper.Domain.Enums;

namespace AssetKeeper.Domain.Entities;

public class AssetHistory
{
    public int Id { get; set; }

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int? ChangedByEmployeeId { get; set; }
    public Employee? ChangedByEmployee { get; set; }

    public DateTime ChangeDate { get; set; } = DateTime.Now;

    public ChangeType ChangeType { get; set; }

    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}