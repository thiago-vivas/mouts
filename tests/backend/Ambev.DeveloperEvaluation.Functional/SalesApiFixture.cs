using System.Net.Http.Headers;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
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
///
/// The sales endpoints require JWT auth, so <see cref="Client"/> carries a real
/// bearer token (minted with the configured signing key); <see cref="AnonymousClient"/>
/// has none, for testing the 401 path.
/// </summary>
public class SalesApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = default!;

    /// <summary>Authenticated client (Bearer token).</summary>
    public HttpClient Client { get; private set; } = default!;

    /// <summary>Client without any token, for unauthorized-path tests.</summary>
    public HttpClient AnonymousClient { get; private set; } = default!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Run as Development so startup seeding (sample sales + default admin) runs,
            // matching the production-gated seeding in Program.cs.
            builder.UseEnvironment("Development");

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

        // Creating a client triggers startup (EnsureCreated + seeding).
        AnonymousClient = _factory.CreateClient();

        // Mint a real JWT (signed with the configured Jwt:SecretKey) for the authenticated client.
        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var token = jwt.GenerateToken(new TestUser());

        Client = _factory.CreateClient();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private sealed class TestUser : IUser
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Username => "functional-tester";
        public string Role => "Admin";
    }
}
