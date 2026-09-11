using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shop.Infrastructure.Persistence;

public sealed class ShopDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Shop")
            ?? "Host=localhost;Port=5433;Database=shop;Username=shop;Password=shop";

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ShopDbContext(options);
    }
}
