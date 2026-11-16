using System.Reflection;
using DomCoreERP.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DomCoreERP.Repository;

public class DomCoreContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentUserService _currentUser;

    public DomCoreContext(
        DbContextOptions<DomCoreContext> options,
        ICurrentUserService currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // ── Tenant query filters, one per entity, explicit ──
        builder.Entity<Product>().HasQueryFilter(p =>
            (_currentUser.IsSuperAdmin && _currentUser.ResolvedCompanyId == null)
            || p.CompanyId == (_currentUser.ResolvedCompanyId ?? _currentUser.CompanyId));

        builder.Entity<Customer>().HasQueryFilter(c =>
            (_currentUser.IsSuperAdmin && _currentUser.ResolvedCompanyId == null)
            || c.CompanyId == (_currentUser.ResolvedCompanyId ?? _currentUser.CompanyId));

        builder.Entity<Invoice>().HasQueryFilter(i =>
            (_currentUser.IsSuperAdmin && _currentUser.ResolvedCompanyId == null)
            || i.CompanyId == (_currentUser.ResolvedCompanyId ?? _currentUser.CompanyId));

        builder.Entity<Payment>().HasQueryFilter(p =>
            (_currentUser.IsSuperAdmin && _currentUser.ResolvedCompanyId == null)
            || p.CompanyId == (_currentUser.ResolvedCompanyId ?? _currentUser.CompanyId));

        builder.Entity<InventoryMovement>().HasQueryFilter(m =>
            (_currentUser.IsSuperAdmin && _currentUser.ResolvedCompanyId == null)
            || m.CompanyId == (_currentUser.ResolvedCompanyId ?? _currentUser.CompanyId));

        // User has nullable CompanyId — SuperAdmin has none
        builder.Entity<User>().HasQueryFilter(u =>
            (_currentUser.IsSuperAdmin && _currentUser.ResolvedCompanyId == null)
            || u.CompanyId == (_currentUser.ResolvedCompanyId ?? _currentUser.CompanyId));

        // ActivityLog has nullable CompanyId — system-wide logs (null) only visible to SA
        builder.Entity<ActivityLog>().HasQueryFilter(a =>
            (_currentUser.IsSuperAdmin && _currentUser.ResolvedCompanyId == null)
            || a.CompanyId == (_currentUser.ResolvedCompanyId ?? _currentUser.CompanyId));
        
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
}