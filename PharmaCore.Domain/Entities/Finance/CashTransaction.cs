using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Shifts;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Finance;

public class CashTransaction : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
