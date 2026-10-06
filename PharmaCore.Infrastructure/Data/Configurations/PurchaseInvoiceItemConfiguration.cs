using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Purchasing;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.BatchNumber)
            .HasMaxLength(100);

        builder.Property(p => p.PurchasePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.SellingPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.SubTotal)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.PurchaseInvoice)
            .WithMany(pi => pi.Items)
            .HasForeignKey(p => p.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Medicine)
            .WithMany()
            .HasForeignKey(p => p.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // New BatchId relationship
        builder.HasOne(p => p.Batch)
            .WithMany(b => b.PurchaseInvoiceItems)
            .HasForeignKey(p => p.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
