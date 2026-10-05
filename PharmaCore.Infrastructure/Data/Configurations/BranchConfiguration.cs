using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Address)
            .HasMaxLength(500);

        builder.Property(b => b.Phone)
            .HasMaxLength(50);

        builder.Property(b => b.ManagerName)
            .HasMaxLength(256);

        // Global Query Filter for Soft Delete
        builder.HasQueryFilter(b => b.IsActive);

        // Relationships
        // Users, Batches, Shifts, Sales, etc., with No Cascade Delete
        builder.HasMany(b => b.Users)
            .WithMany(); // Adjust if ApplicationUser has navigation property to Branch
            
        builder.HasMany(b => b.Batches)
            .WithOne(bat => bat.Branch)
            .HasForeignKey(bat => bat.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Shifts)
            .WithOne(s => s.Branch)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Sales)
            .WithOne(s => s.Branch)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.PurchaseOrders)
            .WithOne(po => po.Branch)
            .HasForeignKey(po => po.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.PurchaseReturns)
            .WithOne(pr => pr.Branch)
            .HasForeignKey(pr => pr.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.OutgoingTransfers)
            .WithOne(st => st.SourceBranch)
            .HasForeignKey(st => st.SourceBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.IncomingTransfers)
            .WithOne(st => st.DestinationBranch)
            .HasForeignKey(st => st.DestinationBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.InventoryTransactions)
            .WithOne(it => it.Branch)
            .HasForeignKey(it => it.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.CashTransactions)
            .WithOne(ct => ct.Branch)
            .HasForeignKey(ct => ct.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.SaleInvoices)
            .WithOne(si => si.Branch)
            .HasForeignKey(si => si.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.PurchaseInvoices)
            .WithOne(pi => pi.Branch)
            .HasForeignKey(pi => pi.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.StockMovements)
            .WithOne(sm => sm.Branch)
            .HasForeignKey(sm => sm.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
