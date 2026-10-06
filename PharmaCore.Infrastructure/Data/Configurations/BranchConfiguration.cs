using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Address)
            .HasMaxLength(500);

        builder.Property(b => b.Phone)
            .HasMaxLength(30);

        builder.Property(b => b.ManagerName)
            .HasMaxLength(256);

        // Branch codes must be unique within a tenant.
        builder.HasIndex(b => new { b.TenantId, b.Code }).IsUnique();

        // NOTE: Global query filters (TenantId + !IsDeleted) are applied generically
        // in ApplicationDbContext. Do NOT add HasQueryFilter here.

        // ─── Relationships ────────────────────────────────────────────────────
        // Configured from the Branch (principal) side. All use Restrict to prevent
        // accidental cascade-deletion of historical operational data.

        builder.HasMany(b => b.Users)
            .WithOne(u => u.Branch)
            .HasForeignKey(u => u.BranchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

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
            .WithOne(st => st.FromBranch)
            .HasForeignKey(st => st.FromBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.IncomingTransfers)
            .WithOne(st => st.ToBranch)
            .HasForeignKey(st => st.ToBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        //builder.HasMany(b => b.InventoryTransactions)
        //    .WithOne(it => it.Branch)
        //    .HasForeignKey(it => it.BranchId)
        //    .OnDelete(DeleteBehavior.Restrict);

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
