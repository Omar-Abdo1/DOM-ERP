using DomCoreERP.Core;
using DomCoreERP.Core.Entities;
using DomCoreERP.Core.Repositories;
using DomCoreERP.Repository;
using DomCoreERP.Repository.DataSeeding;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TouristsAPI.ExtensionsMethod;

public static class ApplicationServicesExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DomCoreContext>(options =>
            options.UseSqlServer(connectionString)
            , 
            ServiceLifetime.Scoped );

        services.AddIdentity<User, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<DomCoreContext>()
            .AddDefaultTokenProviders(); // to generate token for forgetPassword
        
        services.AddHttpContextAccessor();   // singleton
        
        services.AddScoped<RoleSeeder>();
        services.AddScoped<AdminSeeder>();
        services.AddScoped<DbSeeder>();


        // Repositotries :
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        


        // Services :
        services.AddAutoMapper(typeof(MappingProfile));
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        #region Validation Error 
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = (context) =>
            {
                var errors = context.ModelState.Where(P => P.Value.Errors.Count() > 0)
                    .SelectMany(P => P.Value.Errors)
                    .Select(E => E.ErrorMessage)
                    .ToList();
                var ValidationErrorReposonse = new ApiValidationResponse()
                {
                    Errors = errors
                };
                return new BadRequestObjectResult(ValidationErrorReposonse);
        
            };
        }); // For Validation Error 
        #endregion

        return services;
    }
}
