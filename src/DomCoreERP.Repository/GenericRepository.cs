using System.Linq.Expressions;
using DomCoreERP.Core;
using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Pagination;
using DomCoreERP.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DomCoreERP.Repository;

public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntity
{
    protected readonly DomCoreContext _context;

    public GenericRepository(DomCoreContext context)
    {
        _context = context;
    }

    public void Add(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        _context.Set<T>().Add(entity);
    }

    public void Update(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Set<T>().Update(entity);
    }

    public void DeletePermanently(T entity)
        => _context.Set<T>().Remove(entity);

    public void DeletePermanentlyRange(IEnumerable<T> entities)
        => _context.Set<T>().RemoveRange(entities);

    public async Task<T?> GetEntityByConditionAsync(
        Expression<Func<T, bool>> expression,
        bool asNoTracking = false,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _context.Set<T>().AsQueryable();

        if (asNoTracking)
            query = query.AsNoTracking();

        query = query.Where(expression);

        if (includes is not null && includes.Length > 0)
            query = includes.Aggregate(query, (cur, next) => cur.Include(next));

        return await query.FirstOrDefaultAsync();
    }

    public async Task<T?> GetByIdAsync(
        Guid id,
        bool asNoTracking = false,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _context.Set<T>().AsQueryable();

        if (asNoTracking)
            query = query.AsNoTracking();

        query = query.Where(e => e.Id == id);

        if (includes is not null && includes.Length > 0)
            query = includes.Aggregate(query, (cur, next) => cur.Include(next));

        return await query.FirstOrDefaultAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? criteria = null)
    {
        var query = _context.Set<T>().AsQueryable();

        if (criteria is not null)
            query = query.Where(criteria);

        return await query.CountAsync();
    }

    public async Task<IReadOnlyList<T>> GetAllByConditionAsync(
        int? pageIndex = null,
        int? pageSize = null,
        Expression<Func<T, bool>>? criteria = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false,
        bool asNoTracking = false,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _context.Set<T>().AsQueryable();

        if (asNoTracking)
            query = query.AsNoTracking();

        if (criteria is not null)
            query = query.Where(criteria);

        if (orderBy is not null)
            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        if (includes is not null && includes.Length > 0)
            query = includes.Aggregate(query, (cur, next) => cur.Include(next));

        if (pageIndex.HasValue && pageSize.HasValue)
            query = query.Skip((pageIndex.Value - 1) * pageSize.Value).Take(pageSize.Value);

        return await query.ToListAsync();
    }

    public async Task<PagedResult<T>> GetPagedAsync(
        PaginationArg pagination,
        Expression<Func<T, bool>>? criteria = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _context.Set<T>().AsNoTracking().AsQueryable();

        if (criteria is not null)
            query = query.Where(criteria);

        // Count AFTER filtering, BEFORE paging
        var totalCount = await query.CountAsync();

        if (orderBy is not null)
            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        if (includes is not null && includes.Length > 0)
            query = includes.Aggregate(query, (cur, next) => cur.Include(next));

        var items = await query
            .Skip((pagination.PageIndex - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            PageIndex = pagination.PageIndex,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }
}