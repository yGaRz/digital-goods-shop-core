using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Domain;

namespace Shop.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> entity)
    {
        entity.ToTable("ledger_entries");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.OrderId).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Account).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Debit).HasPrecision(18, 2);
        entity.Property(x => x.Credit).HasPrecision(18, 2);
        entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        entity.HasIndex(x => x.OrderId);
        entity.Property(x => x.CreatedAt).IsRequired();
    }
}
