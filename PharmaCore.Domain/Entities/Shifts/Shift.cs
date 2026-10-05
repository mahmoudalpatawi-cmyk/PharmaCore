using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Sales;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Shifts;

public class Shift : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int? ClosedByUserId { get; set; }
    public ApplicationUser? ClosedByUser { get; set; }

    public string CashierId { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }

    public decimal OpeningCash { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal CashDifference { get; set; }

    public decimal StartingCash { get; set; }
    public decimal? EndingCashActual { get; set; }
    public decimal EndingCashSystem { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Difference { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    public string? ClosingNotes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
    public ICollection<SaleInvoice> SaleInvoices { get; set; } = new List<SaleInvoice>();
    public ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}
