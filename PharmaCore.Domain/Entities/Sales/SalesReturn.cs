using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Shifts;

namespace PharmaCore.Domain.Entities.Sales;

public class SalesReturn : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public decimal TotalAmount { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
}
