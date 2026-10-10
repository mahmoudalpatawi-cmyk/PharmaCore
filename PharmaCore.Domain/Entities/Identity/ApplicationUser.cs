using Microsoft.AspNetCore.Identity;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.MultiTenancy;

namespace PharmaCore.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser<int>, IAuditableEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
