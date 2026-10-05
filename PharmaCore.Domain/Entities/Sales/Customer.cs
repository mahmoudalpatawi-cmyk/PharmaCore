using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Insurance;

namespace PharmaCore.Domain.Entities.Sales;

public class Customer : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal Balance { get; set; }
    public int LoyaltyPoints { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<SaleInvoice> Invoices { get; set; } = new List<SaleInvoice>();
    public ICollection<CustomerInsurance> CustomerInsurances { get; set; } = new List<CustomerInsurance>();
    public ICollection<CustomerAccountTransaction> AccountTransactions { get; set; } = new List<CustomerAccountTransaction>();
}
