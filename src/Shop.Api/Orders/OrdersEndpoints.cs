using Microsoft.EntityFrameworkCore;
using Shop.Domain;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Orders;

public sealed record CreateOrderRequest(string Sku);

public sealed record OrderResponse(
    string Id,
    string Sku,
    decimal Amount,
    string Currency,
    string Status,
    string? Code,
    DateTimeOffset CreatedAt)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.Sku,
        order.Amount,
        order.Currency,
        order.Status,
        order.Code,
        order.CreatedAt);
}

public static class OrdersEndpoints
{
    public static RouteGroupBuilder MapOrdersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/orders");

        group.MapPost("/", CreateOrderAsync);
        group.MapGet("/{id}", GetOrderAsync);

        return group;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        ShopDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return Results.BadRequest(new { error = "sku_required" });
        }

        var sku = request.Sku.Trim();
        var product = await db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

        if (product is null)
        {
            return Results.NotFound(new { error = "unknown_sku", sku });
        }

        var order = Order.CreateFromProduct(product, DateTimeOffset.UtcNow);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/orders/{order.Id}", OrderResponse.From(order));
    }

    private static async Task<IResult> GetOrderAsync(
        string id,
        ShopDbContext db,
        CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null
            ? Results.NotFound(new { error = "order_not_found", id })
            : Results.Ok(OrderResponse.From(order));
    }
}
