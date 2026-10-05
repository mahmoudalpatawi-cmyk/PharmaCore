using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Domain.Entities.Purchasing;

public class PurchaseReturn : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public decimal TotalAmount { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}
