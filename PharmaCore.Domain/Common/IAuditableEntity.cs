namespace PharmaCore.Domain.Common;

/// <summary>
/// Non-generic marker interface for auditable entities.
/// Allows a single ChangeTracker loop in DbContext to stamp timestamps
/// across both int-keyed and Guid-keyed entity hierarchies.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    int? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    int? UpdatedBy { get; set; }
}
