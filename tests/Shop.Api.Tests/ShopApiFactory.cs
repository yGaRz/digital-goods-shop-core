using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Shop.Api.Tests;

public sealed class ShopApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Shop", "Host=localhost;Database=unused;Username=x;Password=x");
        builder.UseSetting("Testing:UseInMemoryDatabase", "true");
        builder.UseSetting("Logging:Logstash:Host", "127.0.0.1");
        builder.UseSetting("Logging:Logstash:Port", "1");
    }
}

[CollectionDefinition("ShopApi")]
public sealed class ShopApiCollection : ICollectionFixture<ShopApiFactory>;
