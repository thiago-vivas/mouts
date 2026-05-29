using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

/// <summary>
/// Base notification handler for domain events. Logs the event to the application
/// log (Serilog) and appends it to the <see cref="IEventStore"/> (MongoDB). No
/// real message broker is used — this satisfies the "publish events" differential
/// by logging + persisting, as the challenge allows.
/// </summary>
public abstract class DomainEventHandlerBase<TEvent> : INotificationHandler<TEvent>
    where TEvent : DomainEvent
{
    private readonly ILogger _logger;
    private readonly IEventStore _eventStore;

    protected DomainEventHandlerBase(ILogger logger, IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Domain event published: {EventType} (EventId={EventId}, OccurredAt={OccurredAt}) {@Payload}",
            notification.EventType, notification.EventId, notification.OccurredAt, notification);

        await _eventStore.AppendAsync(notification, cancellationToken);
    }
}
