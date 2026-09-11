using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Buyer.Api.Tests;

public sealed class PurchasesTests : IClassFixture<BuyerApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public PurchasesTests(BuyerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_200_when_shop_reachable()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_known_sku_returns_created_with_order_id()
    {
        var response = await _client.PostAsJsonAsync("/purchases", new { sku = "STEAM-TOPUP-500" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PurchaseDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("ord_test_buyer_001", body.OrderId);
        Assert.Equal("STEAM-TOPUP-500", body.Sku);
        Assert.Equal(500m, body.Amount);
        Assert.Equal("RUB", body.Currency);
        Assert.Equal("created", body.Status);
    }

    [Fact]
    public async Task Purchase_unknown_sku_returns_404()
    {
        var response = await _client.PostAsJsonAsync("/purchases", new { sku = "NO-SUCH-SKU" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_empty_sku_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/purchases", new { sku = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record PurchaseDto(
        string OrderId,
        string Sku,
        decimal Amount,
        string Currency,
        string Status);
}
