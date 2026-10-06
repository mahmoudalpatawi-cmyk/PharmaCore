using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Clinical;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Insurance;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Shifts;

namespace PharmaCore.Domain.Entities.Sales;

public class Sale : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? InsuranceCompanyId { get; set; }
    public InsuranceCompany? InsuranceCompany { get; set; }

    public int? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal InsuranceCoverageAmount { get; set; }
    public decimal Discount { get; set; }
    public int PointsRedeemed { get; set; }
    public int PointsEarned { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<SaleInvoice> Invoices { get; set; } = new List<SaleInvoice>();
}
