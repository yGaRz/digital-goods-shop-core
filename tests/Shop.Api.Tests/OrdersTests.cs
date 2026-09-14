using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Shop.Api.Tests;

[Collection("ShopApi")]
public sealed class OrdersTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public OrdersTests(ShopApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Products_seed_contains_twelve_skus_including_steam_500()
    {
        var response = await _client.GetAsync("/products");
        response.EnsureSuccessStatusCode();

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        Assert.NotNull(products);
        Assert.Equal(12, products.Count);
        Assert.Contains(products, p => p.Sku == "STEAM-TOPUP-500" && p.Price == 500m);
    }

    [Fact]
    public async Task Create_order_for_known_sku_returns_created_with_price_snapshot()
    {
        var response = await _client.PostAsJsonAsync("/orders", new { sku = "STEAM-TOPUP-500" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        Assert.NotNull(order);
        Assert.StartsWith("ord_", order.Id);
        Assert.Equal("STEAM-TOPUP-500", order.Sku);
        Assert.Equal(500m, order.Amount);
        Assert.Equal("RUB", order.Currency);
        Assert.Equal("created", order.Status);

        var get = await _client.GetAsync($"/orders/{order.Id}");
        get.EnsureSuccessStatusCode();
        var loaded = await get.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        Assert.NotNull(loaded);
        Assert.Equal(order.Id, loaded.Id);
        Assert.Equal(500m, loaded.Amount);
    }

    [Fact]
    public async Task Create_order_for_unknown_sku_returns_404()
    {
        var response = await _client.PostAsJsonAsync("/orders", new { sku = "NO-SUCH-SKU" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_unknown_order_returns_404()
    {
        var response = await _client.GetAsync("/orders/ord_does_not_exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Two_creates_without_idempotency_key_make_two_orders()
    {
        var first = await _client.PostAsJsonAsync("/orders", new { sku = "STEAM-TOPUP-500" });
        var second = await _client.PostAsJsonAsync("/orders", new { sku = "STEAM-TOPUP-500" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var a = await first.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        var b = await second.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotEqual(a.Id, b.Id);
    }

    private sealed record ProductDto(string Sku, string Name, string Type, decimal Price, string Currency);

    private sealed record OrderDto(
        string Id,
        string Sku,
        decimal Amount,
        string Currency,
        string Status,
        string? Code,
        DateTimeOffset CreatedAt);
}
