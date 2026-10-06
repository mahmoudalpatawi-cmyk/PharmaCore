using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Auditing;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        // String Constraints
        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(128); // Supports Guid as string, or int

        // These are JSON payloads, so they can remain as NVARCHAR(MAX) or specific lengths. 
        // We'll leave them without MaxLength to allow large JSON objects, or define a generous max.
        // We do not apply MaxLength here to allow EF Core to use NVARCHAR(MAX) for JSON strings.

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        // Note: Global query filters (TenantId) applied in DbContext.

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
