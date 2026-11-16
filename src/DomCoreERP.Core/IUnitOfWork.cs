using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Repositories;

using Microsoft.EntityFrameworkCore;

namespace DomCoreERP.Core;

public interface IUnitOfWork : IAsyncDisposable
{
    IGenericRepository<T> Repository<T>() where T : class, IEntity;
    
    DbContext Context { get; }

    Task<int> CompleteAsync();
}