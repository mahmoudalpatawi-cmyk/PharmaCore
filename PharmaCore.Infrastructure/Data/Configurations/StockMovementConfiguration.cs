using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(sm => sm.Id);

        // Ignore the alias properties to prevent EF Core from creating duplicate foreign keys
        builder.Ignore(sm => sm.MedicineBatchId);
        builder.Ignore(sm => sm.MedicineBatch);

        // Properties
        builder.Property(sm => sm.Reason).HasMaxLength(500);
        builder.Property(sm => sm.ReferenceInvoiceId).HasMaxLength(100);

        // Global Query Filter for Soft Delete (Inherits from BaseEntity, but does it have IsActive?)
        // StockMovement inherits from BaseEntity which has IsActive
        builder.HasQueryFilter(sm => sm.IsActive);

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
