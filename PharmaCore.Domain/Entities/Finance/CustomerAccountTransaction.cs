using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Domain.Entities.Finance;

public class CustomerAccountTransaction : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
