namespace AssetKeeper.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}