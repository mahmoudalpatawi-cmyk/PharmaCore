using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class SaleInvoiceItemConfiguration : IEntityTypeConfiguration<SaleInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SaleInvoiceItem> builder)
    {
        builder.HasKey(sii => sii.Id);

        // Monetary Precision
        builder.Property(sii => sii.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(sii => sii.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(sii => sii.Discount).HasColumnType("decimal(18,2)");

        // NOTE: Global Query Filters for Soft Delete/Tenancy are applied in ApplicationDbContext.

        // Relationships
        builder.HasOne(sii => sii.SaleInvoice)
            .WithMany(si => si.Items)
            .HasForeignKey(sii => sii.SaleInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sii => sii.Batch)
            .WithMany()
            .HasForeignKey(sii => sii.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
