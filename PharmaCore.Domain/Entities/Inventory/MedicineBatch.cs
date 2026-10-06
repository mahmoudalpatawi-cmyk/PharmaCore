namespace PharmaCore.Domain.Entities.Inventory;

/// <summary>
/// DEPRECATED — Do not use. This class is intentionally empty and excluded from the EF Core model.
/// It was an alias for Batch that caused a Table-Per-Hierarchy (TPH) Discriminator column
/// to be silently added, breaking all queries. The as-cast pattern it enabled always returned null.
/// 
/// Use <see cref="Batch"/> directly for all inventory batch operations.
/// This file is retained only to prevent compiler errors in files not yet migrated.
/// It will be removed in the next major version.
/// </summary>
[Obsolete("Use Batch instead. This class is not mapped to the database.", error: false)]
public sealed class MedicineBatch
{
    // Intentionally empty. This type is excluded from the EF Core model via modelBuilder.Ignore<MedicineBatch>().
}
