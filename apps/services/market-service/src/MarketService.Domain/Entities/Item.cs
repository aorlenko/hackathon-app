namespace MarketService.Domain.Entities;

public sealed class Item
{
    public Guid ItemId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal ReferencePrice { get; set; }
    public bool IsTradable { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
