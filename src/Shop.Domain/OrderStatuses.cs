namespace Shop.Domain;

public static class OrderStatuses
{
    public const string Created = "created";
    public const string Paid = "paid";
    public const string Delivering = "delivering";
    public const string Delivered = "delivered";
    public const string PaymentFailed = "payment_failed";
    public const string OutOfStock = "out_of_stock";
    public const string DeliveryFailed = "delivery_failed";
}
