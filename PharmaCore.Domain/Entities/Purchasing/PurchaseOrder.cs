using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Purchasing;

public class PurchaseOrder : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<PurchaseReturn> Returns { get; set; } = new List<PurchaseReturn>();
}
