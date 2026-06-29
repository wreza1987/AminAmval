namespace AssetKeeper.Domain.Entities;

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // رابطه با اموال
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}