using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.ORM.Events;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Events;

/// <summary>
/// Covers the MongoDB event store's graceful-degradation path: when no Mongo
/// connection is configured it must behave as a no-op rather than throwing, so
/// the API can run without MongoDB available.
/// </summary>
public class MongoEventStoreTests
{
    private sealed class SampleEvent : DomainEvent
    {
        public override string EventType => "Sample";
    }

    [Fact(DisplayName = "AppendAsync is a no-op when Mongo is not configured")]
    public async Task Given_NoConfig_When_Append_Then_DoesNotThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build(); // no MongoDb section
        var store = new MongoEventStore(configuration);

        // Act
        var act = () => store.AppendAsync(new SampleEvent());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "Constructing with a Mongo connection string initializes the collection")]
    public void Given_Config_When_Constructed_Then_DoesNotThrow()
    {
        // Arrange
        // The Mongo driver connects lazily, so constructing with a valid-looking
        // connection string exercises the configured branch without a live server.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:Database"] = "developer_evaluation"
            })
            .Build();

        // Act
        var act = () => new MongoEventStore(configuration);

        // Assert
        act.Should().NotThrow();
    }
}
