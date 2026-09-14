using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shop.Domain;
using Shop.Infrastructure.Persistence;

namespace Shop.Api.Payments;

public sealed record PaymentWebhookRequest(
    [property: JsonPropertyName("event_id")] string EventId,
    [property: JsonPropertyName("order_id")] string OrderId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

public static class PaymentWebhookEndpoints
{
    public static RouteGroupBuilder MapPaymentWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/webhook");
        group.MapPost("/payment", HandlePaymentWebhookAsync);
        return group;
    }

    private static async Task<IResult> HandlePaymentWebhookAsync(
        PaymentWebhookRequest request,
        ShopDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventId) ||
            string.IsNullOrWhiteSpace(request.OrderId) ||
            string.IsNullOrWhiteSpace(request.Status))
        {
            return Results.BadRequest(new { error = "invalid_webhook" });
        }

        var eventId = request.EventId.Trim();
        var orderId = request.OrderId.Trim();
        var status = request.Status.Trim().ToLowerInvariant();

        if (status is not (PaymentEventStatuses.Paid or PaymentEventStatuses.Failed))
        {
            return Results.BadRequest(new { error = "invalid_status", status });
        }

        var existing = await db.PaymentEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (existing is not null)
        {
            Log.Information(
                "Payment webhook duplicate event_id {EventId} order_id {OrderId} outcome {Outcome}",
                eventId,
                orderId,
                existing.Outcome);
            return Results.Ok(new { accepted = true, duplicate = true, outcome = existing.Outcome });
        }

        await using var tx = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var paymentEvent = new PaymentEvent
            {
                EventId = eventId,
                OrderId = orderId,
                Status = status,
                Amount = request.Amount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "RUB" : request.Currency.Trim(),
                CreatedAt = request.CreatedAt,
                ReceivedAt = DateTimeOffset.UtcNow,
                Outcome = PaymentEventOutcomes.Ignored
            };
            db.PaymentEvents.Add(paymentEvent);

            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order is null)
            {
                paymentEvent.Outcome = PaymentEventOutcomes.OrderMissing;
                await db.SaveChangesAsync(cancellationToken);
                if (tx is not null)
                {
                    await tx.CommitAsync(cancellationToken);
                }

                Log.Information(
                    "Payment webhook stored before order event_id {EventId} order_id {OrderId} reason {Reason}",
                    eventId,
                    orderId,
                    "order_missing");
                return Results.Ok(new { accepted = true, outcome = paymentEvent.Outcome });
            }

            if (status == PaymentEventStatuses.Paid)
            {
                ApplyPaid(db, paymentEvent, order);
            }
            else
            {
                ApplyFailed(paymentEvent, order);
            }

            await db.SaveChangesAsync(cancellationToken);
            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            Log.Information(
                "Payment webhook processed event_id {EventId} order_id {OrderId} status {Status} outcome {Outcome} order_status {OrderStatus}",
                eventId,
                orderId,
                status,
                paymentEvent.Outcome,
                order.Status);

            return Results.Ok(new { accepted = true, outcome = paymentEvent.Outcome, orderStatus = order.Status });
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            Log.Information(
                "Payment webhook race duplicate event_id {EventId} order_id {OrderId}",
                eventId,
                orderId);
            return Results.Ok(new { accepted = true, duplicate = true, outcome = PaymentEventOutcomes.Duplicate });
        }
    }

    private static void ApplyPaid(
        ShopDbContext db,
        PaymentEvent paymentEvent,
        Order order)
    {
        if (order.Status != OrderStatuses.Created)
        {
            paymentEvent.Outcome = PaymentEventOutcomes.Ignored;
            Log.Information(
                "Payment webhook paid ignored for order {OrderId} current status {Status} event_id {EventId}",
                order.Id,
                order.Status,
                paymentEvent.EventId);
            return;
        }

        if (paymentEvent.Amount != order.Amount ||
            !string.Equals(paymentEvent.Currency, order.Currency, StringComparison.OrdinalIgnoreCase))
        {
            paymentEvent.Outcome = PaymentEventOutcomes.AmountMismatch;
            Log.Information(
                "Payment webhook amount mismatch event_id {EventId} order_id {OrderId} reason {Reason} webhook_amount {WebhookAmount} order_amount {OrderAmount}",
                paymentEvent.EventId,
                order.Id,
                "amount_mismatch",
                paymentEvent.Amount,
                order.Amount);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        order.Status = OrderStatuses.Paid;
        paymentEvent.Outcome = PaymentEventOutcomes.AppliedPaid;

        db.LedgerEntries.Add(LedgerEntry.DebitEntry(
            order.Id,
            LedgerAccounts.BankAsset,
            order.Amount,
            $"{paymentEvent.EventId}:{LedgerAccounts.BankAsset}",
            now));
        db.LedgerEntries.Add(LedgerEntry.CreditEntry(
            order.Id,
            LedgerAccounts.Escrow,
            order.Amount,
            $"{paymentEvent.EventId}:{LedgerAccounts.Escrow}",
            now));
    }

    private static void ApplyFailed(PaymentEvent paymentEvent, Order order)
    {
        if (order.Status != OrderStatuses.Created)
        {
            paymentEvent.Outcome = PaymentEventOutcomes.Ignored;
            Log.Information(
                "Payment webhook late failed ignored order_id {OrderId} status {Status} event_id {EventId}",
                order.Id,
                order.Status,
                paymentEvent.EventId);
            return;
        }

        order.Status = OrderStatuses.PaymentFailed;
        paymentEvent.Outcome = PaymentEventOutcomes.AppliedFailed;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("unique", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || message.Contains("IX_", StringComparison.OrdinalIgnoreCase);
    }
}
