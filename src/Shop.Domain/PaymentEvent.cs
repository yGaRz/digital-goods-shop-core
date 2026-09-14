namespace Shop.Domain;

public sealed class PaymentEvent
{
    public required string EventId { get; init; }
    public required string OrderId { get; init; }
    public required string Status { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public required string Outcome { get; set; }
}

public static class PaymentEventStatuses
{
    public const string Paid = "paid";
    public const string Failed = "failed";
}

public static class PaymentEventOutcomes
{
    public const string AppliedPaid = "applied_paid";
    public const string AppliedFailed = "applied_failed";
    public const string AmountMismatch = "amount_mismatch";
    public const string OrderMissing = "order_missing";
    public const string Ignored = "ignored";
    public const string Duplicate = "duplicate";
}
