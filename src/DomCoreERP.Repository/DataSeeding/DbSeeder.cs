using DomCoreERP.Core.Config;
using DomCoreERP.Core.Constants;
using DomCoreERP.Core.Entities;
using DomCoreERP.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DomCoreERP.Repository.DataSeeding;

public class DbSeeder
{
    private static readonly Guid CompanyAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid AdminAId   = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminBId   = Guid.Parse("b1111111-1111-1111-1111-111111111111");

    private static readonly Guid[] EmployeeAIds = {
        Guid.Parse("a2222222-0000-0000-0000-000000000001"),
        Guid.Parse("a2222222-0000-0000-0000-000000000002"),
        Guid.Parse("a2222222-0000-0000-0000-000000000003"),
        Guid.Parse("a2222222-0000-0000-0000-000000000004"),
        Guid.Parse("a2222222-0000-0000-0000-000000000005"),
    };
    private static readonly Guid[] EmployeeBIds = {
        Guid.Parse("b2222222-0000-0000-0000-000000000001"),
        Guid.Parse("b2222222-0000-0000-0000-000000000002"),
        Guid.Parse("b2222222-0000-0000-0000-000000000003"),
        Guid.Parse("b2222222-0000-0000-0000-000000000004"),
        Guid.Parse("b2222222-0000-0000-0000-000000000005"),
    };

    private readonly DomCoreContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SeedSettings _settings;

    public DbSeeder(
        DomCoreContext context,
        UserManager<User> userManager,
        IOptions<SeedSettings> settings)
    {
        _context = context;
        _userManager = userManager;
        _settings = settings.Value;
    }

    public async Task SeedAsync()
    {
        if (await _context.Companies.IgnoreQueryFilters().AnyAsync(c => c.Id == CompanyAId))
            return;

        // ── Companies ──
        _context.Companies.AddRange(
            new Company
            {
                Id = CompanyAId, Name = "Company A", Description = "First demo company",
                Email = "info@companya.com", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow,
            },
            new Company
            {
                Id = CompanyBId, Name = "Company B", Description = "Second demo company",
                Email = "info@companyb.com", Status = CompanyStatus.Active, CreatedAt = DateTime.UtcNow,
            });

        // ── Admins ──
        await CreateUserAsync(AdminAId, "Admin A", "admin.a@companya.com", Roles.CompanyAdmin, CompanyAId);
        await CreateUserAsync(AdminBId, "Admin B", "admin.b@companyb.com", Roles.CompanyAdmin, CompanyBId);

        // ── Employees ──
        var namesA = new[] { "Ahmed A", "Sara A", "Omar A", "Mona A", "Yousef A" };
        var namesB = new[] { "Kareem B", "Laila B", "Tarek B", "Nour B", "Hassan B" };

        for (int i = 0; i < 5; i++)
        {
            await CreateUserAsync(EmployeeAIds[i], namesA[i], $"emp.a{i + 1}@companya.com", Roles.Employee, CompanyAId);
            await CreateUserAsync(EmployeeBIds[i], namesB[i], $"emp.b{i + 1}@companyb.com", Roles.Employee, CompanyBId);
        }

        // ── Products ──
        _context.Products.AddRange(
            NewProduct(CompanyAId, "iPhone 15",   "IP15-A",  999m,  50m, 10m),
            NewProduct(CompanyAId, "AirPods Pro", "AP-A",    249m,  30m,  5m),
            NewProduct(CompanyAId, "iPad Air",    "IPADA-A", 599m,  20m,  5m),
            NewProduct(CompanyBId, "Samsung S24", "SS24-B",  899m,  40m, 10m),
            NewProduct(CompanyBId, "Galaxy Buds", "GB-B",    199m,  60m, 15m));

        // ── Customers ──
        _context.Customers.AddRange(
            NewCustomer(CompanyAId, "Mohamed Ahmed",  "01001111111"),
            NewCustomer(CompanyAId, "Fatima Ali",     "01002222222"),
            NewCustomer(CompanyBId, "Mohamed Ahmed",  "01001111111"),
            NewCustomer(CompanyBId, "Khaled Ibrahim", "01003333333"));

        await _context.SaveChangesAsync();
    }

    private async Task CreateUserAsync(
        Guid id, string fullName, string email, string role, Guid companyId)
    {
        var user = new User
        {
            Id = id,
            UserName = email,
            Email = email,
            FullName = fullName,
            CompanyId = companyId,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, _settings.DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to seed user {email}: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException(
                $"Failed to assign role {role} to {email}: " +
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }

    private static Product NewProduct(
        Guid companyId, string name, string sku,
        decimal sellingPrice, decimal stock, decimal minStock) => new()
    {
        CompanyId = companyId, Name = name, SKU = sku,
        SellingPrice = sellingPrice, CurrentStock = stock, MinimumStock = minStock,
        IsActive = true, CreatedAt = DateTime.UtcNow,
    };

    private static Customer NewCustomer(Guid companyId, string name, string phone) => new()
    {
        CompanyId = companyId, Name = name, Phone = phone,
        IsActive = true, CreatedAt = DateTime.UtcNow,
    };
}