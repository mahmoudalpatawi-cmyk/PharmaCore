using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Catalog;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.HasKey(m => m.Id);

        // Properties
        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.TradeName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.GenericName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Barcode)
            .HasMaxLength(100);

        builder.Property(m => m.PrimaryUnit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.SecondaryUnit)
            .HasMaxLength(50);

        builder.Property(m => m.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(50);

        // Monetary
        builder.Property(m => m.SellingPrice)
            .HasColumnType("decimal(18,2)");

        // Global Query Filter for Soft Delete
        builder.HasQueryFilter(m => m.IsActive);

        // Relationships
        builder.HasOne(m => m.Category)
            .WithMany(c => c.Medicines)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.ActiveIngredients)
            .WithOne(ai => ai.Medicine)
            .HasForeignKey(ai => ai.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Batches)
            .WithOne(b => b.Medicine)
            .HasForeignKey(b => b.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.PurchaseOrderItems)
            .WithOne(poi => poi.Medicine)
            .HasForeignKey(poi => poi.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.PrescriptionItems)
            .WithOne(pi => pi.Medicine)
            .HasForeignKey(pi => pi.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
