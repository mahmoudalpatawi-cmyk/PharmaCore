using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class SaleInvoiceConfiguration : IEntityTypeConfiguration<SaleInvoice>
{
    public void Configure(EntityTypeBuilder<SaleInvoice> builder)
    {
        builder.HasKey(si => si.Id);

        builder.Property(si => si.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(si => si.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(si => si.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(si => si.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(si => si.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(si => si.RemainingAmount).HasColumnType("decimal(18,2)");

        // InvoiceNumber should be unique within a tenant
        builder.HasIndex(si => new { si.TenantId, si.InvoiceNumber }).IsUnique();

        builder.HasOne(si => si.Sale)
            .WithMany(s => s.Invoices)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Customer)
            .WithMany()
            .HasForeignKey(si => si.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Branch)
            .WithMany(b => b.SaleInvoices)
            .HasForeignKey(si => si.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Shift)
            .WithMany(sh => sh.SaleInvoices)
            .HasForeignKey(si => si.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
