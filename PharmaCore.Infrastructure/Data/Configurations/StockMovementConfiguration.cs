using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(sm => sm.Id);

        // Properties
        builder.Property(sm => sm.Reason).HasMaxLength(500);
        builder.Property(sm => sm.ReferenceDocumentId).HasMaxLength(100);

        // NOTE: Global Query Filters for Soft Delete/Tenancy are applied in ApplicationDbContext.

        // Relationships
        builder.HasOne(sm => sm.Branch)
            .WithMany(b => b.StockMovements)
            .HasForeignKey(sm => sm.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.Batch)
            .WithMany()
            .HasForeignKey(sm => sm.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
