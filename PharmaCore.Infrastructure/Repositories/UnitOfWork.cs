using System;
using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Domain.Common;
using PharmaCore.Infrastructure.Data;

namespace PharmaCore.Infrastructure.Repositories;

/// <summary>
/// Concrete Unit of Work implementation delegating SaveChangesAsync to
/// the shared scoped ApplicationDbContext.
///
/// DESIGN: Because both IUnitOfWork and IRepository&lt;T&gt; are registered as
/// Scoped, and ApplicationDbContext is also Scoped, all repositories and
/// this UnitOfWork resolve the SAME DbContext instance within a single
/// request scope. Calling SaveChangesAsync here commits ALL pending changes
/// tracked by any repository that has called AddAsync, UpdateAsync, or
/// DeleteAsync in this request.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Persists all tracked changes to the database in a single transaction.
    /// Returns the number of affected rows as returned by EF Core.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
