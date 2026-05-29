using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.ORM.Events;

/// <summary>
/// No-op event bus used when no message broker (RabbitMQ) is configured, so the
/// API runs without one. Events are still logged and persisted to the event store.
/// </summary>
public class NullEventBus : IEventBus
{
    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
