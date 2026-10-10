using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.CostPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.SellingPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        // Add a database check constraint to ensure quantity is never negative
        builder.ToTable(t => t.HasCheckConstraint("CK_Batch_Quantity_NonNegative", "[Quantity] >= 0"));

        builder.HasOne(b => b.Branch)
            .WithMany(br => br.Batches)
            .HasForeignKey(b => b.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Medicine)
            .WithMany(m => m.Batches)
            .HasForeignKey(b => b.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Supplier)
            .WithMany()
            .HasForeignKey(b => b.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
