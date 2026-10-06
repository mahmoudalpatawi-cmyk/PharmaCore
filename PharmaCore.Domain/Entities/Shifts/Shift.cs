using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Sales;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Shifts;

/// <summary>
/// Represents a cashier's work session at a specific branch.
/// 
/// FIXES APPLIED:
/// 1. Removed CashierId (string, no FK) and CashierName (denormalized string).
///    The cashier is now exclusively identified by OpenedByUserId → ApplicationUser.
///    This eliminates the dual-reference inconsistency where UserId could be null
///    while CashierId was populated.
/// 2. Renamed UserId → OpenedByUserId for explicit semantics (differentiates from ClosedByUserId).
/// 3. Removed duplicate cash balance columns. Previous code had:
///    OpeningCash / StartingCash (same), ExpectedCash / EndingCashSystem (same),
///    ClosingCash / EndingCashActual (same), CashDifference / Difference (same).
///    The canonical names are retained below.
/// </summary>
public class Shift : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>The user who opened/started the shift.</summary>
    public int? OpenedByUserId { get; set; }
    public ApplicationUser? OpenedByUser { get; set; }

    /// <summary>The user who closed the shift. May differ from opener (e.g., supervisor closes).</summary>
    public int? ClosedByUserId { get; set; }
    public ApplicationUser? ClosedByUser { get; set; }

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }

    /// <summary>Cash in the drawer at the start of the shift (manually counted).</summary>
    public decimal OpeningCash { get; set; }

    /// <summary>Actual physical cash counted at shift close.</summary>
    public decimal ClosingCashActual { get; set; }

    /// <summary>System-calculated expected cash at close (OpeningCash + cash sales - cash refunds - cash expenses).</summary>
    public decimal ClosingCashSystem { get; set; }

    /// <summary>Variance: ClosingCashActual - ClosingCashSystem. Negative means drawer is short.</summary>
    public decimal CashDifference { get; set; }

    public decimal TotalSales { get; set; }
    public decimal TotalExpenses { get; set; }

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
