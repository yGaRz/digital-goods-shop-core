using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shop.Domain;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Tests;

[Collection("ShopApi")]
public sealed class PaymentsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ShopApiFactory _factory;
    private readonly HttpClient _client;

    public PaymentsTests(ShopApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Paid_webhook_moves_order_to_paid_and_posts_escrow()
    {
        var orderId = await CreateOrderAsync("STEAM-TOPUP-500");

        var response = await _client.PostAsJsonAsync("/webhook/payment", new
        {
            event_id = $"evt_{Guid.NewGuid():N}",
            order_id = orderId,
            status = "paid",
            amount = 500m,
            currency = "RUB",
            created_at = DateTimeOffset.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await GetOrderAsync(orderId);
        Assert.Equal("paid", order.Status);
        Assert.Null(order.Code);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var entries = db.LedgerEntries.Where(e => e.OrderId == orderId).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(500m, entries.Where(e => e.Account == LedgerAccounts.BankAsset).Sum(e => e.Debit - e.Credit));
        Assert.Equal(500m, entries.Where(e => e.Account == LedgerAccounts.Escrow).Sum(e => e.Credit - e.Debit));
        Assert.DoesNotContain(entries, e => e.Account.StartsWith("seller_payable", StringComparison.Ordinal));
        Assert.Equal(0m, entries.Where(e => e.Account == LedgerAccounts.PlatformFee).Sum(e => e.Credit - e.Debit));
    }

    [Fact]
    public async Task Failed_webhook_sets_payment_failed_without_escrow()
    {
        var orderId = await CreateOrderAsync("STEAM-TOPUP-500");

        var response = await _client.PostAsJsonAsync("/webhook/payment", new
        {
            event_id = $"evt_{Guid.NewGuid():N}",
            order_id = orderId,
            status = "failed",
            amount = 500m,
            currency = "RUB",
            created_at = DateTimeOffset.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await GetOrderAsync(orderId);
        Assert.Equal("payment_failed", order.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Empty(db.LedgerEntries.Where(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Amount_mismatch_returns_200_and_keeps_created()
    {
        var orderId = await CreateOrderAsync("KEY-CS2-PRIME");

        var response = await _client.PostAsJsonAsync("/webhook/payment", new
        {
            event_id = $"evt_{Guid.NewGuid():N}",
            order_id = orderId,
            status = "paid",
            amount = 1m,
            currency = "RUB",
            created_at = DateTimeOffset.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WebhookResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("amount_mismatch", body.Outcome);

        var order = await GetOrderAsync(orderId);
        Assert.Equal("created", order.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Empty(db.LedgerEntries.Where(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Duplicate_event_id_is_idempotent()
    {
        var orderId = await CreateOrderAsync("STEAM-TOPUP-500");
        var eventId = $"evt_{Guid.NewGuid():N}";
        var payload = new
        {
            event_id = eventId,
            order_id = orderId,
            status = "paid",
            amount = 500m,
            currency = "RUB",
            created_at = DateTimeOffset.UtcNow
        };

        var first = await _client.PostAsJsonAsync("/webhook/payment", payload);
        var second = await _client.PostAsJsonAsync("/webhook/payment", payload);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Equal(2, db.LedgerEntries.Count(e => e.OrderId == orderId));
        Assert.Equal(1, db.PaymentEvents.Count(e => e.EventId == eventId));
    }

    private async Task<string> CreateOrderAsync(string sku)
    {
        var response = await _client.PostAsJsonAsync("/orders", new { sku });
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        Assert.NotNull(order);
        return order.Id;
    }

    private async Task<OrderDto> GetOrderAsync(string id)
    {
        var response = await _client.GetAsync($"/orders/{id}");
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        Assert.NotNull(order);
        return order;
    }

    private sealed record OrderDto(
        string Id,
        string Sku,
        decimal Amount,
        string Currency,
        string Status,
        string? Code,
        DateTimeOffset CreatedAt);

    private sealed record WebhookResult(bool Accepted, string Outcome);
}
