using DomCoreERP.Core.Constants;
using Microsoft.AspNetCore.Identity;

namespace DomCoreERP.Repository.DataSeeding;

public class RoleSeeder
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RoleSeeder(RoleManager<IdentityRole<Guid>> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        foreach (var roleName in Roles.All)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await _roleManager.CreateAsync(
                new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = roleName });

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}