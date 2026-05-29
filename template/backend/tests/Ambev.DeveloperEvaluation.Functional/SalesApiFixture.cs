using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

/// <summary>No-op event store so functional tests don't require a running MongoDB.</summary>
public class NullEventStore : IEventStore
{
    public Task AppendAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>
/// Boots the real Web API in-memory (WebApplicationFactory) for end-to-end HTTP
/// tests. The database is swapped for the EF Core in-memory provider and the
/// event store for a no-op, so the whole pipeline (routing, model binding,
/// validation, MediatR handlers, error middleware, response envelopes) is
/// exercised without external infrastructure.
/// </summary>
public class SalesApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = default!;
    public HttpClient Client { get; private set; } = default!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace the relational DbContext with an in-memory one.
                services.RemoveAll(typeof(DbContextOptions<DefaultContext>));
                services.RemoveAll(typeof(DbContextOptions));
                services.AddDbContext<DefaultContext>(options =>
                    options.UseInMemoryDatabase("functional-tests"));

                // Replace the Mongo event store with a no-op.
                services.RemoveAll<IEventStore>();
                services.AddSingleton<IEventStore, NullEventStore>();
            });
        });

        // Creating the client triggers startup (EnsureCreated + seeding).
        Client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
