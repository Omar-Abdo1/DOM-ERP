using System.Linq.Expressions;
using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Pagination;

namespace DomCoreERP.Core.Repositories;

public interface IGenericRepository<T> where T : class, IEntity
{
    void Add(T entity);
    void Update(T entity);
    void DeletePermanently(T entity);
    void DeletePermanentlyRange(IEnumerable<T> entities);

    Task<T?> GetEntityByConditionAsync(
        Expression<Func<T, bool>> expression,
        bool asNoTracking = false,
        params Expression<Func<T, object>>[] includes);

    Task<T?> GetByIdAsync(
        Guid id,
        bool asNoTracking = false,
        params Expression<Func<T, object>>[] includes);

    Task<int> CountAsync(Expression<Func<T, bool>>? criteria = null);

    Task<IReadOnlyList<T>> GetAllByConditionAsync(
        int? pageIndex = null,
        int? pageSize = null,
        Expression<Func<T, bool>>? criteria = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false,
        bool asNoTracking = false,
        params Expression<Func<T, object>>[] includes);
    
    Task<PagedResult<T>> GetPagedAsync(
        PaginationArg pagination,
        Expression<Func<T, bool>>? criteria = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false,
        params Expression<Func<T, object>>[] includes);
}