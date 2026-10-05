using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Domain.Entities.Identity;

public class ApplicationUser : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
