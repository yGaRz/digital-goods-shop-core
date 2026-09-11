using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Domain;

namespace Shop.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("orders");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasMaxLength(64);
        entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Amount).HasPrecision(18, 2);
        entity.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
        entity.Property(x => x.RequestId).HasMaxLength(128);
        entity.Property(x => x.SellerId).HasMaxLength(64);
        entity.Property(x => x.Code).HasMaxLength(64);
        entity.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"Code\" IS NOT NULL");
        entity.Property(x => x.CreatedAt).IsRequired();
    }
}
