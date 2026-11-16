using System.Threading.RateLimiting;

using DomCoreERP.API.ExtensionsMethod;
using DomCoreERP.Core.Config;
using DomCoreERP.Core.Entities.Email;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TouristsAPI.ExtensionsMethod;


namespace TouristsAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            // Add the converter to use string names for all enums
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
        
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen();
        builder.Services.AddOpenApi();

        builder.Services.AddApplicationServices(
            builder.Configuration.GetConnectionString("DefaultConnection"));
        
        builder.Services.AddJWTServices(builder.Configuration["JWT:SecretKey"],
            builder.Configuration["JWT:IssuerIP"]);
        
        
        builder.Services.AddSwaggerAdvanced("DOM-ERP-API");
        
        builder.Services.Configure<SeedSettings>(builder.Configuration.GetSection("Seed"));
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("My Policy", options =>
            {
                options.AllowAnyHeader();
                options.AllowAnyMethod();
                options.WithOrigins(builder.Configuration["FrontBaseUrl"]); // URL for The FrontEnd
                options.AllowCredentials(); // for SignalR
            });
        });
        
        // builder.Services.AddHangfire(config => config
        //     .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        //     .UseSimpleAssemblyNameTypeSerializer()
        //     .UseRecommendedSerializerSettings()
        //     .UseSqlServerStorage(builder.
        //         Configuration.GetConnectionString("DefaultConnection")));
        /*
         *"This configures Hangfire to store jobs in SQL Server so they persist after restarts.
         * The Serializer settings ensure that my C# objects are converted to JSON and back correctly,
         * even if I update my application version."
         */

        //builder.Services.AddHangfireServer();  
        
        builder.Services.Configure<EmailSettings>(
            builder.Configuration.GetSection("EmailSettings")
        ); // go get the values from app settings.json


        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
        
        builder.Services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsJsonAsync(new { Message = "Too many requests" });
            };
            
            options.AddPolicy("Global", context =>
            {
                string userId = context.Connection.RemoteIpAddress?.ToString()??"unknown";
                
                return RateLimitPartition.GetFixedWindowLimiter(
                    userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 80,            
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy("Strict", context =>
            {
                string userId = context.Connection.RemoteIpAddress?.ToString()??"unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
            
        });

      //  builder.Services.AddSignalR(); // for Real-Time Application
        
        
        
        var app = builder.Build();
        
        await app.UpdateDatabaseAsync();

        app.UseMiddleware<ExceptionMiddleWare>(); // custom MiddleWare

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapOpenApi();
        }
        
        app.UseCors("My  Policy");
        app.UseStaticFiles();

        app.UseRateLimiter();
        
        app.UseStatusCodePagesWithReExecute("/error/{0}");

      //  app.UseSerilogRequestLogging();
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        
       // app.UseHangfireDashboard("/hangfire");
        
      //  app.MapHub<ChatHub>("/hubs/chat");

       // app.AddingrecurringJobs();
        
        await app.RunAsync();
    }
}