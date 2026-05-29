using Ambev.DeveloperEvaluation.Domain.Events;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.ORM.Events;

/// <summary>
/// Publishes domain events to a message broker (RabbitMQ) via Rebus pub/sub.
/// The event is published under its concrete type so subscribers can route on it.
/// </summary>
public class RebusEventBus : IEventBus
{
    private readonly IBus _bus;

    public RebusEventBus(IBus bus)
    {
        _bus = bus;
    }

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
        => _bus.Publish(domainEvent);
}
