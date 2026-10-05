using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Purchasing;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Reason)
            .HasMaxLength(500);

        builder.Property(p => p.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.PurchaseReturn)
            .WithMany(pr => pr.Items)
            .HasForeignKey(p => p.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Batch)
            .WithMany(b => b.PurchaseReturnItems)
            .HasForeignKey(p => p.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
