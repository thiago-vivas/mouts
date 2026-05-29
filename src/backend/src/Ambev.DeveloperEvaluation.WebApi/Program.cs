using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Common.HealthChecks;
using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Seeding;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Ambev.DeveloperEvaluation.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Log.Information("Starting web application");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.AddDefaultLogging();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.AddBasicHealthChecks();
            builder.Services.AddSwaggerGen(options =>
            {
                // Add a JWT bearer scheme so Swagger UI shows an "Authorize" button
                // and attaches the token to the secured Sales endpoints.
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste the JWT returned by POST /api/auth (no \"Bearer \" prefix needed)."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // CORS for the Angular frontend (origins configurable via Cors:AllowedOrigins).
            var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200" };
            builder.Services.AddCors(options => options.AddPolicy("FrontendCors", policy =>
                policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

            builder.Services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
                )
            );

            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.RegisterDependencies();

            builder.Services.AddAutoMapper(typeof(Program).Assembly, typeof(ApplicationLayer).Assembly);

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(ApplicationLayer).Assembly,
                    typeof(Program).Assembly
                );
            });

            // Register all FluentValidation validators so the ValidationBehavior pipeline
            // validates every command/query at the boundary (defence-in-depth alongside the
            // explicit checks in the handlers). Done by reflection because the
            // FluentValidation.DependencyInjectionExtensions package isn't referenced.
            var validatorRegistrations = new[] { typeof(ApplicationLayer).Assembly, typeof(Program).Assembly }
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type is { IsAbstract: false, IsInterface: false })
                .SelectMany(type => type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .Select(i => (ServiceType: i, ImplementationType: type)));
            foreach (var (serviceType, implementationType) in validatorRegistrations)
                builder.Services.AddScoped(serviceType, implementationType);

            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            var app = builder.Build();

            // Apply pending migrations and seed sample data on startup.
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();
                if (context.Database.IsRelational())
                    context.Database.Migrate();
                else
                    context.Database.EnsureCreated();

                // Seed sample data and the default admin user only outside production,
                // so a well-known credential is never created in a production database.
                if (app.Environment.IsDevelopment())
                    DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
            }

            app.UseMiddleware<ErrorHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("FrontendCors");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseBasicHealthChecks();

            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            // Also write to stderr directly: startup failures can happen before the
            // Serilog sinks are configured, so Log.Fatal alone may produce no output.
            Console.Error.WriteLine($"FATAL: application terminated unexpectedly:{Environment.NewLine}{ex}");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
