namespace Shop.Domain;

public sealed class Order
{
    public required string Id { get; init; }
    public required string Sku { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; set; }
    public string? RequestId { get; set; }
    public string? SellerId { get; set; }
    public string? Code { get; set; }
    public DateTimeOffset CreatedAt { get; init; }

    public static Order CreateFromProduct(Product product, DateTimeOffset now)
    {
        var id = $"ord_{Guid.NewGuid():N}";
        return new Order
        {
            Id = id,
            Sku = product.Sku,
            Amount = product.Price,
            Currency = product.Currency,
            Status = OrderStatuses.Created,
            CreatedAt = now
        };
    }
}
