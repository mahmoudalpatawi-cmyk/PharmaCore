using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;

namespace PharmaCore.Domain.Entities.Auditing;

/// <summary>
/// Immutable audit record for any create/update/delete operation on any entity.
/// 
/// CRITICAL FIX: EntityId is now string (was int) so it can store both
/// int and Guid primary keys without type-conversion failures at runtime.
/// OldValues and NewValues store JSON-serialized property snapshots.
/// </summary>
public class AuditLog : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    /// <summary>FK to the user who performed the action. Nullable: system operations may have no user.</summary>
    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>The action performed: "Created", "Updated", "Deleted", "SoftDeleted".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>The CLR type name of the entity that was modified (e.g., "Medicine", "Batch").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// The primary key of the modified entity, stored as a string.
    /// Supports both int PKs (e.g., "42") and Guid PKs (e.g., "3fa85f64-5717-4562-b3fc-2c963f66afa6").
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON snapshot of property values before the change. Null on Create operations.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of property values after the change. Null on Delete operations.</summary>
    public string? NewValues { get; set; }

    /// <summary>IP address of the client that initiated the request.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Precise UTC timestamp of the audit event.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
