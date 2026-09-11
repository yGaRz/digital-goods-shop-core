using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Buyer.Api.Purchases;

public sealed record PurchaseRequest(string Sku);

public sealed record PurchaseResponse(
    string OrderId,
    string Sku,
    decimal Amount,
    string Currency,
    string Status);

public sealed record ShopOrderResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status);

public interface IShopOrdersClient
{
    Task<ShopOrderCreateResult> CreateOrderAsync(string sku, CancellationToken cancellationToken);
}

public sealed record ShopOrderCreateResult(
    bool Success,
    int StatusCode,
    ShopOrderResponse? Order,
    string? ErrorBody);

public sealed class ShopOrdersClient(IHttpClientFactory httpClientFactory) : IShopOrdersClient
{
    public async Task<ShopOrderCreateResult> CreateOrderAsync(string sku, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("shop");
        using var response = await client.PostAsJsonAsync("/orders", new { sku }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ShopOrderCreateResult(false, (int)response.StatusCode, null, errorBody);
        }

        var order = await response.Content.ReadFromJsonAsync<ShopOrderResponse>(cancellationToken: cancellationToken);
        return new ShopOrderCreateResult(true, (int)response.StatusCode, order, null);
    }
}

public static class PurchasesEndpoints
{
    public static RouteGroupBuilder MapPurchasesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/purchases");
        group.MapPost("/", CreatePurchaseAsync);
        return group;
    }

    private static async Task<IResult> CreatePurchaseAsync(
        PurchaseRequest request,
        IShopOrdersClient shop,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return Results.BadRequest(new { error = "sku_required" });
        }

        var sku = request.Sku.Trim();
        logger.LogInformation("Buyer purchase started for sku {Sku}", sku);

        var result = await shop.CreateOrderAsync(sku, cancellationToken);
        if (!result.Success || result.Order is null)
        {
            logger.LogWarning(
                "Shop rejected purchase sku {Sku} with HTTP {StatusCode}",
                sku,
                result.StatusCode);

            return result.StatusCode switch
            {
                StatusCodes.Status404NotFound => Results.NotFound(new { error = "unknown_sku", sku }),
                StatusCodes.Status400BadRequest => Results.BadRequest(new { error = "shop_bad_request", detail = result.ErrorBody }),
                _ => Results.Problem(
                    detail: result.ErrorBody,
                    statusCode: result.StatusCode >= 400 ? result.StatusCode : StatusCodes.Status502BadGateway,
                    title: "shop_unavailable")
            };
        }

        var order = result.Order;
        using (logger.BeginScope(new Dictionary<string, object> { ["order_id"] = order.Id }))
        {
            logger.LogInformation(
                "Buyer purchase created order {OrderId} sku {Sku} status {Status}",
                order.Id,
                order.Sku,
                order.Status);
        }

        return Results.Created(
            $"/purchases/{order.Id}",
            new PurchaseResponse(order.Id, order.Sku, order.Amount, order.Currency, order.Status));
    }
}
