namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Publishes domain events to an external message broker. Implemented over Rebus
/// (RabbitMQ). When no broker is configured the implementation is a no-op, so the
/// API runs without a broker (events are still logged and stored).
/// </summary>
public interface IEventBus
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
