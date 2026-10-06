using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaCore.Domain.Entities.Shifts;

namespace PharmaCore.Infrastructure.Data.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ClosingNotes)
            .HasMaxLength(1000);

        builder.Property(s => s.OpeningCash).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ClosingCashActual).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ClosingCashSystem).HasColumnType("decimal(18,2)");
        builder.Property(s => s.CashDifference).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TotalSales).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TotalExpenses).HasColumnType("decimal(18,2)");

        builder.HasOne(s => s.Branch)
            .WithMany(b => b.Shifts)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.OpenedByUser)
            .WithMany()
            .HasForeignKey(s => s.OpenedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ClosedByUser)
            .WithMany()
            .HasForeignKey(s => s.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
