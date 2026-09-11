namespace Shop.Domain;

public sealed class Product
{
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public decimal Price { get; init; }
    public required string Currency { get; init; }
}
