using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmaCore.Domain.Common;
using PharmaCore.Infrastructure.Data;

namespace PharmaCore.Infrastructure.Repositories;

/// <summary>
/// Generic EF Core repository implementing IRepository&lt;T&gt;.
///
/// DESIGN DECISIONS:
/// - Uses the scoped ApplicationDbContext injected via DI. All repositories
///   and the UnitOfWork share the SAME context instance per request, which
///   ensures all tracked changes are committed in a single transaction by
///   UnitOfWork.SaveChangesAsync.
/// - Mutation methods (AddAsync, UpdateAsync, DeleteAsync) do NOT call
///   SaveChangesAsync. Persistence is the responsibility of IUnitOfWork,
///   matching the explicit contract defined by the existing Application services.
/// - Global query filters (tenant isolation + soft-delete) defined in
///   ApplicationDbContext are automatically applied to all queries. This
///   repository does NOT bypass them.
/// - DeleteAsync performs a SOFT delete for entities implementing ISoftDelete,
///   stamping IsDeleted = true and DeletedAt = UtcNow. For entities that do
///   not implement ISoftDelete, it performs a hard (physical) delete.
///   This matches the domain design where most tenant-owned entities use
///   soft deletion and the global filter already excludes IsDeleted records.
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Finds an entity by its integer primary key.
    /// Uses a LINQ query instead of FindAsync to ensure EF Core global query filters
    /// are ALWAYS applied, even if the entity is already in the change tracker.
    /// </summary>
    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Returns all entities matching the predicate, subject to global query
    /// filters. Results are not tracked (AsNoTracking) because this is a
    /// read-only list operation. Tracked reads go through GetByIdAsync.
    /// </summary>
    public async Task<IEnumerable<T>> ListAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a paged subset of entities matching the predicate along with the total count.
    /// Uses database-level CountAsync and Skip/Take queries with AsNoTracking.
    /// Global query filters (tenant isolation and soft-delete) remain fully effective.
    /// </summary>
    public Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return GetPagedAsync(predicate, pageNumber, pageSize, null, cancellationToken);
    }

    /// <summary>
    /// Returns a paged subset of entities matching the predicate along with the total count,
    /// with an optional custom ordering expression. Defaults to OrderBy(e => e.Id) when orderBy is null.
    /// Uses database-level CountAsync and Skip/Take queries with AsNoTracking.
    /// Global query filters (tenant isolation and soft-delete) remain fully effective.
    /// </summary>
    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(predicate);
        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = orderBy != null
            ? orderBy(query)
            : query.OrderBy(e => e.Id);

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Adds an entity to the change tracker. Does not persist until
    /// IUnitOfWork.SaveChangesAsync is called.
    /// </summary>
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Marks an entity as modified in the change tracker. Does not persist
    /// until IUnitOfWork.SaveChangesAsync is called.
    /// </summary>
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an entity.
    /// - If the entity implements ISoftDelete: sets IsDeleted = true and
    ///   DeletedAt = UtcNow. The entity remains in the database and is
    ///   excluded from future queries by the global query filter.
    /// - If the entity does NOT implement ISoftDelete: performs a physical
    ///   (hard) delete.
    /// Does not persist until IUnitOfWork.SaveChangesAsync is called.
    /// </summary>
    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            softDeleteEntity.DeletedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
        }
        else
        {
            _dbSet.Remove(entity);
        }

        return Task.CompletedTask;
    }
}
