using Microsoft.EntityFrameworkCore;
using Shop.Domain;

namespace Shop.Infrastructure.Persistence;

public static class ProductCatalogSeed
{
    public static readonly IReadOnlyList<Product> Products =
    [
        new() { Sku = "STEAM-TOPUP-500", Name = "Пополнение Steam 500 ₽", Type = "topup", Price = 500m, Currency = "RUB" },
        new() { Sku = "STEAM-TOPUP-1000", Name = "Пополнение Steam 1000 ₽", Type = "topup", Price = 1000m, Currency = "RUB" },
        new() { Sku = "STEAM-TOPUP-2500", Name = "Пополнение Steam 2500 ₽", Type = "topup", Price = 2500m, Currency = "RUB" },
        new() { Sku = "KEY-CS2-PRIME", Name = "CS2 Prime Status ключ", Type = "key", Price = 1290m, Currency = "RUB" },
        new() { Sku = "KEY-GTA5", Name = "GTA V ключ активации", Type = "key", Price = 1990m, Currency = "RUB" },
        new() { Sku = "KEY-EFT", Name = "Escape from Tarkov ключ", Type = "key", Price = 3490m, Currency = "RUB" },
        new() { Sku = "SUB-DISCORD-1M", Name = "Discord Nitro 1 месяц", Type = "subscription", Price = 399m, Currency = "RUB" },
        new() { Sku = "SUB-YT-3M", Name = "YouTube Premium 3 месяца", Type = "subscription", Price = 1490m, Currency = "RUB" },
        new() { Sku = "SUB-SPOTIFY-1M", Name = "Spotify Premium 1 месяц", Type = "subscription", Price = 299m, Currency = "RUB" },
        new() { Sku = "GIFT-PSN-1000", Name = "PlayStation Store карта 1000 ₽", Type = "giftcard", Price = 1000m, Currency = "RUB" },
        new() { Sku = "GIFT-XBOX-1500", Name = "Xbox Gift Card 1500 ₽", Type = "giftcard", Price = 1500m, Currency = "RUB" },
        new() { Sku = "GIFT-ROBLOX-800", Name = "Roblox 800 Robux", Type = "giftcard", Price = 890m, Currency = "RUB" }
    ];

    public static async Task EnsureSeededAsync(ShopDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Products.AddRange(Products);
        await db.SaveChangesAsync(cancellationToken);
    }
}
