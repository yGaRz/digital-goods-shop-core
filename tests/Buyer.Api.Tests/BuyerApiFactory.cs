using Buyer.Api.Purchases;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Buyer.Api.Tests;

public sealed class BuyerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Shop:BaseUrl", "http://shop.test");
        builder.UseSetting("Logging:Logstash:Host", "127.0.0.1");
        builder.UseSetting("Logging:Logstash:Port", "1");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IShopOrdersClient>();
            services.AddSingleton<IShopOrdersClient, FakeShopOrdersClient>();

            services.AddHttpClient("shop")
                .ConfigurePrimaryHttpMessageHandler(() => new AlwaysHealthyShopHandler());
        });
    }

    private sealed class AlwaysHealthyShopHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("Healthy")
            });
        }
    }
}

internal sealed class FakeShopOrdersClient : IShopOrdersClient
{
    public Task<ShopOrderCreateResult> CreateOrderAsync(string sku, CancellationToken cancellationToken)
    {
        if (sku == "NO-SUCH-SKU")
        {
            return Task.FromResult(new ShopOrderCreateResult(
                false,
                404,
                null,
                """{"error":"unknown_sku"}"""));
        }

        var order = new ShopOrderResponse(
            "ord_test_buyer_001",
            sku,
            500m,
            "RUB",
            "created");

        return Task.FromResult(new ShopOrderCreateResult(true, 201, order, null));
    }
}
