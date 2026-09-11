using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Infrastructure.Persistence;

namespace Shop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShopInfrastructure(
        this IServiceCollection services,
        string connectionString,
        bool useInMemoryDatabase = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (useInMemoryDatabase)
        {
            services.AddDbContext<ShopDbContext>(options =>
                options.UseInMemoryDatabase("shop-lab"));
        }
        else
        {
            services.AddDbContext<ShopDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgres");
        }

        if (useInMemoryDatabase)
        {
            services.AddHealthChecks();
        }

        return services;
    }

    public static async Task InitializeShopDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        await ProductCatalogSeed.EnsureSeededAsync(db, cancellationToken);
    }
}
