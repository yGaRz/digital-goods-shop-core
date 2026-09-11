using Microsoft.Extensions.DependencyInjection;

namespace Shop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShopInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        return services;
    }
}
