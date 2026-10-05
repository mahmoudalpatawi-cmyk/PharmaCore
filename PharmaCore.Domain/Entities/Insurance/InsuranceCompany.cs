using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Domain.Entities.Insurance;

public class InsuranceCompany : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<CustomerInsurance> CustomerInsurances { get; set; } = new List<CustomerInsurance>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
