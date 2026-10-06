using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        // Monetary precision
        builder.Property(s => s.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.InsuranceCoverageAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.Discount)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.NetAmount)
            .HasColumnType("decimal(18,2)");

        // NOTE: Global Query Filters for Soft Delete/Tenancy are applied in ApplicationDbContext.

        // Relationships
        builder.HasOne(s => s.Shift)
            .WithMany(sh => sh.Sales)
            .HasForeignKey(s => s.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Branch)
            .WithMany(b => b.Sales)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.InsuranceCompany)
            .WithMany(ic => ic.Sales)
            .HasForeignKey(s => s.InsuranceCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Sale)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Payments)
            .WithOne(p => p.Sale)
            .HasForeignKey(p => p.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.SalesReturns)
            .WithOne(sr => sr.Sale)
            .HasForeignKey(sr => sr.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Prescriptions)
            .WithOne(p => p.Sale)
            .HasForeignKey(p => p.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(s => s.Invoices)
            .WithOne(si => si.Sale)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
