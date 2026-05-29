using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.Events;

/// <summary>
/// MongoDB-backed implementation of <see cref="IEventStore"/>. Appends every
/// published domain event as an immutable document to the <c>sale_events</c>
/// collection. If Mongo is not configured the store degrades to a no-op so the
/// API can still run (events are still logged by the logging handler).
/// </summary>
public class MongoEventStore : IEventStore
{
    private readonly IMongoCollection<BsonDocument>? _collection;

    public MongoEventStore(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var database = configuration["MongoDb:Database"];

        if (!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(database))
        {
            var client = new MongoClient(connectionString);
            _collection = client.GetDatabase(database).GetCollection<BsonDocument>("sale_events");
        }
    }

    public async Task AppendAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (_collection is null)
            return;

        var payloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        var document = new BsonDocument
        {
            { "eventId", domainEvent.EventId.ToString() },
            { "type", domainEvent.EventType },
            { "occurredAt", domainEvent.OccurredAt },
            { "payload", BsonDocument.Parse(payloadJson) }
        };

        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
