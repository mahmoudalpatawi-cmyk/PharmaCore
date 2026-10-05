using PharmaCore.Domain.Common;

namespace PharmaCore.Domain.Entities.MultiTenancy;

public class Tenant : BaseEntity<Guid>, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime SubscriptionEndDate { get; set; }
    public int MaxBranches { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
