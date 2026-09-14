using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Bank.Api.Payments;

public sealed record CreatePaymentRequest(
    string OrderId,
    string Status,
    decimal? Amount = null,
    string? Currency = null);

public sealed record PaymentAcceptedResponse(
    string EventId,
    string OrderId,
    string Status,
    decimal Amount,
    string Currency,
    object? Shop);

internal sealed record ShopOrderResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status);

public static class PaymentsEndpoints
{
    public static RouteGroupBuilder MapPaymentsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/payments");
        group.MapPost("/", CreatePaymentAsync);
        return group;
    }

    private static async Task<IResult> CreatePaymentAsync(
        CreatePaymentRequest request,
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return Results.BadRequest(new { error = "order_id_required" });
        }

        var status = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
        if (status is not ("paid" or "failed"))
        {
            return Results.BadRequest(new { error = "invalid_status", status = request.Status });
        }

        var orderId = request.OrderId.Trim();
        var client = httpClientFactory.CreateClient("shop");

        decimal amount;
        string currency;
        if (request.Amount is { } overrideAmount)
        {
            amount = overrideAmount;
            currency = string.IsNullOrWhiteSpace(request.Currency) ? "RUB" : request.Currency.Trim();
        }
        else
        {
            using var orderResponse = await client.GetAsync($"/orders/{orderId}", cancellationToken);
            if (orderResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound(new { error = "order_not_found", orderId });
            }

            if (!orderResponse.IsSuccessStatusCode)
            {
                var detail = await orderResponse.Content.ReadAsStringAsync(cancellationToken);
                return Results.Problem(
                    detail: detail,
                    statusCode: (int)orderResponse.StatusCode,
                    title: "shop_order_lookup_failed");
            }

            var order = await orderResponse.Content.ReadFromJsonAsync<ShopOrderResponse>(cancellationToken: cancellationToken);
            if (order is null)
            {
                return Results.Problem(title: "shop_order_unreadable", statusCode: StatusCodes.Status502BadGateway);
            }

            amount = order.Amount;
            currency = order.Currency;
        }

        var eventId = $"evt_{Guid.NewGuid():N}";
        var createdAt = DateTimeOffset.UtcNow;
        var webhookBody = new
        {
            event_id = eventId,
            order_id = orderId,
            status,
            amount,
            currency,
            created_at = createdAt
        };

        logger.LogInformation(
            "Bank payment emit event_id {EventId} order_id {OrderId} status {Status} amount {Amount}",
            eventId,
            orderId,
            status,
            amount);

        using var webhookResponse = await client.PostAsJsonAsync("/webhook/payment", webhookBody, cancellationToken);
        var shopBody = webhookResponse.Content.Headers.ContentType?.MediaType?.Contains("json") == true
            ? await webhookResponse.Content.ReadFromJsonAsync<object>(cancellationToken: cancellationToken)
            : await webhookResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!webhookResponse.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Shop webhook rejected event_id {EventId} HTTP {StatusCode}",
                eventId,
                (int)webhookResponse.StatusCode);
            return Results.Problem(
                detail: shopBody?.ToString(),
                statusCode: (int)webhookResponse.StatusCode,
                title: "shop_webhook_failed");
        }

        return Results.Ok(new PaymentAcceptedResponse(eventId, orderId, status, amount, currency, shopBody));
    }
}
