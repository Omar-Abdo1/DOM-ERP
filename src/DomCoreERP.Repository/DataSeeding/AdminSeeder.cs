using DomCoreERP.Core.Config;
using DomCoreERP.Core.Constants;
using DomCoreERP.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DomCoreERP.Repository.DataSeeding;

public class AdminSeeder
{
    private static readonly Guid SuperAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly DomCoreContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SeedSettings _settings;

    public AdminSeeder(
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
        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == SuperAdminId))
            return;

        var s = _settings.SuperAdmin;
        if (string.IsNullOrWhiteSpace(s.Email) || string.IsNullOrWhiteSpace(s.Password))
            throw new InvalidOperationException(
                "Seed:SuperAdmin Email/Password not configured in appsettings.Development.json.");

        var superAdmin = new User
        {
            Id = SuperAdminId,
            UserName = s.Email,
            Email = s.Email,
            FullName = s.FullName,
            CompanyId = null,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(superAdmin, s.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to seed SuperAdmin: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        var roleResult = await _userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException(
                "Failed to assign SuperAdmin role: " +
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }
}