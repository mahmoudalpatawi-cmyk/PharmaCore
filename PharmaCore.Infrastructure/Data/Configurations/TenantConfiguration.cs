using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);

        // Tenant codes must be globally unique.
        builder.HasIndex(t => t.Code).IsUnique();

        // NOTE: Global query filter (!IsDeleted) is applied generically in ApplicationDbContext.
        // Tenant does NOT implement IMustHaveTenant (it IS the tenant root).
        // Its filter applies only ISoftDelete: !IsDeleted.

        // Relationships
        builder.HasMany(t => t.Branches)
            .WithOne(b => b.Tenant)
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
