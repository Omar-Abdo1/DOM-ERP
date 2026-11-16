using DomCoreERP.Repository;
using DomCoreERP.Repository.DataSeeding;
using Microsoft.EntityFrameworkCore;

using TouristsAPI;

namespace DomCoreERP.API.ExtensionsMethod;

public static class UpdatingDataBase
{
    public static async Task UpdateDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying migrations...");
            await services.GetRequiredService<DomCoreContext>().Database.MigrateAsync();

            logger.LogInformation("Seeding roles...");
            await services.GetRequiredService<RoleSeeder>().SeedAsync();       // 1st

            logger.LogInformation("Seeding Super Admin...");
            await services.GetRequiredService<AdminSeeder>().SeedAsync();      // 2nd

            logger.LogInformation("Seeding demo data...");
            await services.GetRequiredService<DbSeeder>().SeedAsync();         // 3rd

            logger.LogInformation("Database ready.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during migration or seeding.");
            throw;   // crash the app — never start with a broken DB silently
        }
    }
}