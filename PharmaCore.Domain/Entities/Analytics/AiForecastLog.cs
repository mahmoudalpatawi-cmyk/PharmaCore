using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Domain.Entities.Analytics;

public class AiForecastLog : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public DateTime ForecastedRunOutDate { get; set; }
    public int RecommendedReorderQuantity { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
