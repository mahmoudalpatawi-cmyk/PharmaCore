using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Domain.Entities.Inventory;

public class InventoryTransaction : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
