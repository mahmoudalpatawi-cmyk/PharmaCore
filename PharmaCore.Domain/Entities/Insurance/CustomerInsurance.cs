using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Domain.Entities.Insurance;

public class CustomerInsurance : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int InsuranceCompanyId { get; set; }
    public InsuranceCompany? InsuranceCompany { get; set; }

    public string MemberNumber { get; set; } = string.Empty;
    public decimal CoveragePercentage { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
