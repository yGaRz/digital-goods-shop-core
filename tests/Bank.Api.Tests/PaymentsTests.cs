using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bank.Api.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Bank.Api.Tests;

public sealed class BankApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Shop:BaseUrl", "http://shop.test");
        builder.UseSetting("Logging:Logstash:Host", "127.0.0.1");
        builder.UseSetting("Logging:Logstash:Port", "1");

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient("shop")
                .ConfigurePrimaryHttpMessageHandler(() => new FakeShopHandler());
        });
    }
}

internal sealed class FakeShopHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (request.Method == HttpMethod.Get && path == "/health")
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Healthy") };
        }

        if (request.Method == HttpMethod.Get && path.StartsWith("/orders/", StringComparison.Ordinal))
        {
            var id = path["/orders/".Length..];
            var json = JsonSerializer.Serialize(new
            {
                id,
                sku = "STEAM-TOPUP-500",
                amount = 500m,
                currency = "RUB",
                status = "created",
                code = (string?)null,
                createdAt = DateTimeOffset.UtcNow
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        if (request.Method == HttpMethod.Post && path == "/webhook/payment")
        {
            var payload = await request.Content!.ReadFromJsonAsync<PaymentWebhookRequest>(cancellationToken: cancellationToken);
            var outcome = payload?.Status == "failed" ? "applied_failed" : "applied_paid";
            var json = JsonSerializer.Serialize(new { accepted = true, outcome, orderStatus = payload?.Status == "failed" ? "payment_failed" : "paid" });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private sealed record PaymentWebhookRequest(string Status);
}

public sealed class PaymentsTests : IClassFixture<BankApiFactory>
{
    private readonly HttpClient _client;

    public PaymentsTests(BankApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Payment_paid_returns_event_id()
    {
        var response = await _client.PostAsJsonAsync("/payments", new CreatePaymentRequest("ord_test", "paid"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaymentAcceptedResponse>();
        Assert.NotNull(body);
        Assert.StartsWith("evt_", body.EventId);
        Assert.Equal("ord_test", body.OrderId);
        Assert.Equal("paid", body.Status);
        Assert.Equal(500m, body.Amount);
    }

    [Fact]
    public async Task Payment_with_amount_override_forwards_custom_amount()
    {
        var response = await _client.PostAsJsonAsync(
            "/payments",
            new CreatePaymentRequest("ord_test", "paid", Amount: 1m, Currency: "RUB"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaymentAcceptedResponse>();
        Assert.NotNull(body);
        Assert.Equal(1m, body.Amount);
    }
}
