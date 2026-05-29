using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Events;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
        builder.Services.AddSingleton<IEventStore, MongoEventStore>();

        // Domain events are published to RabbitMQ via Rebus only when a broker is
        // configured (e.g. in docker-compose). Otherwise we fall back to a no-op
        // bus so the API runs without a broker — events are still logged + stored.
        var rabbitConnection = builder.Configuration["RabbitMq:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(rabbitConnection))
        {
            builder.Services.AddRebus(configure => configure
                .Transport(t => t.UseRabbitMqAsOneWayClient(rabbitConnection)));
            builder.Services.AddScoped<IEventBus, RebusEventBus>();
        }
        else
        {
            builder.Services.AddSingleton<IEventBus, NullEventBus>();
        }
    }
}