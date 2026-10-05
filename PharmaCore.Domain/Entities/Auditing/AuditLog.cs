using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;

namespace PharmaCore.Domain.Entities.Auditing;

public class AuditLog : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
}
