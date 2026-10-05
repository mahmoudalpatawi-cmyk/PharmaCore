using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Inventory;

public class StockTransfer : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int FromBranchId { get; set; }
    public Branch? FromBranch { get; set; }

    public int ToBranchId { get; set; }
    public Branch? ToBranch { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Pending;
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}
