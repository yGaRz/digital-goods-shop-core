using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Domain;

namespace Shop.Infrastructure.Persistence.Configurations;

public sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> entity)
    {
        entity.ToTable("payment_events");
        entity.HasKey(x => x.EventId);
        entity.Property(x => x.EventId).HasMaxLength(64);
        entity.Property(x => x.OrderId).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Status).HasMaxLength(16).IsRequired();
        entity.Property(x => x.Amount).HasPrecision(18, 2);
        entity.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.ReceivedAt).IsRequired();
        entity.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
        entity.HasIndex(x => x.OrderId);
    }
}
