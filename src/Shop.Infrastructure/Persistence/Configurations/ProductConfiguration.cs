using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Domain;

namespace Shop.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("products");
        entity.HasKey(x => x.Sku);
        entity.Property(x => x.Sku).HasMaxLength(64);
        entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
        entity.Property(x => x.Type).HasMaxLength(32).IsRequired();
        entity.Property(x => x.Price).HasPrecision(18, 2);
        entity.Property(x => x.Currency).HasMaxLength(8).IsRequired();
    }
}
