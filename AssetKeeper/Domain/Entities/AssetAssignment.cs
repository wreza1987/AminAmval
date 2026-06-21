namespace AssetKeeper.Domain.Entities;

public class AssetAssignment
{
    public int Id { get; set; }

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime AssignmentDate { get; set; }
    public DateTime? ReturnDate { get; set; }        // اگر خالی باشد یعنی هنوز تحویل است

    public string? DeliveredBy { get; set; }         // کسی که تحویل داده
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}