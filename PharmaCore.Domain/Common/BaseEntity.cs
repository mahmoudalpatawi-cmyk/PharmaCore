namespace PharmaCore.Domain.Common;

/// <summary>
/// Generic base entity. All database-mapped domain entities inherit from this.
/// 
/// DESIGN DECISIONS:
/// - IsActive has been REMOVED. Soft-delete is handled exclusively via ISoftDelete
///   (IsDeleted + DeletedAt). Conflating two booleans (IsActive/IsDeleted) for
///   the same concept caused contradictory query filters and undefined behavior.
/// - CreatedBy/UpdatedBy are int? (FK to ApplicationUser.Id, which is int).
///   They are not enforced as hard FKs in the database to avoid circular dependency
///   issues on user deletion, but they map unambiguously to the user table's PK type.
/// - Implements IAuditableEntity so ApplyAuditInformation() in DbContext can use a
///   single loop for all entity types (both int-keyed and Guid-keyed).
/// </summary>
public abstract class BaseEntity<TId> : IAuditableEntity
{
    public TId Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

/// <summary>
/// Convenience base for the majority of entities that use an int primary key.
/// </summary>
public abstract class BaseEntity : BaseEntity<int>
{
}
