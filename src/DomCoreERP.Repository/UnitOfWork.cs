// Repository/UnitOfWork.cs
using System.Collections;
using DomCoreERP.Core;
using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Repositories;

using Microsoft.EntityFrameworkCore;

namespace DomCoreERP.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly DomCoreContext _context;
    private readonly Hashtable _repositories = new();

    public UnitOfWork(DomCoreContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IEntity
    {
        var typeName = typeof(T).Name;

        if (!_repositories.ContainsKey(typeName))
            _repositories[typeName] = new GenericRepository<T>(_context);

        return (IGenericRepository<T>)_repositories[typeName]!;
    }

    public DbContext Context { get =>  _context;}

    public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();
}