using Microsoft.EntityFrameworkCore;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Catalog;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapProductsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/products");
        group.MapGet("/", async (ShopDbContext db, CancellationToken ct) =>
        {
            var products = await db.Products.AsNoTracking()
                .OrderBy(p => p.Sku)
                .Select(p => new
                {
                    p.Sku,
                    p.Name,
                    p.Type,
                    p.Price,
                    p.Currency
                })
                .ToListAsync(ct);

            return Results.Ok(products);
        });

        return group;
    }
}
