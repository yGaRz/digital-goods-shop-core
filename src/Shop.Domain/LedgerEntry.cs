namespace Shop.Domain;

public sealed class LedgerEntry
{
    public long Id { get; init; }
    public required string OrderId { get; init; }
    public required string Account { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public required string IdempotencyKey { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static LedgerEntry DebitEntry(
        string orderId,
        string account,
        decimal amount,
        string idempotencyKey,
        DateTimeOffset now) =>
        new()
        {
            OrderId = orderId,
            Account = account,
            Debit = amount,
            Credit = 0m,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now
        };

    public static LedgerEntry CreditEntry(
        string orderId,
        string account,
        decimal amount,
        string idempotencyKey,
        DateTimeOffset now) =>
        new()
        {
            OrderId = orderId,
            Account = account,
            Debit = 0m,
            Credit = amount,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now
        };
}

public static class LedgerAccounts
{
    public const string BankAsset = "bank_asset";
    public const string Escrow = "escrow";
    public const string PlatformFee = "platform_fee";
    public const string RefundPending = "refund_pending";

    public static string SellerPayable(string sellerId) => $"seller_payable:{sellerId}";
}
