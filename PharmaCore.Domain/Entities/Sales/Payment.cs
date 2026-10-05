using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Shifts;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Sales;

public class Payment : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
