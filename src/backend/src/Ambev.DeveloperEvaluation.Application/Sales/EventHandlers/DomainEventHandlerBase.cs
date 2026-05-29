using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

/// <summary>
/// Base notification handler for domain events. Logs the event (Serilog), appends
/// it to the <see cref="IEventStore"/> (MongoDB audit log), and publishes it to the
/// message broker via <see cref="IEventBus"/> (Rebus/RabbitMQ — a no-op when no
/// broker is configured).
///
/// Dispatch is best-effort: the event is raised <i>after</i> the sale has already
/// been committed, so a transient outage of the audit store or broker must not fail
/// the command (which would surface a 500 on an operation that actually succeeded).
/// Failures are logged for follow-up instead of propagating.
/// </summary>
public abstract class DomainEventHandlerBase<TEvent> : INotificationHandler<TEvent>
    where TEvent : DomainEvent
{
    private readonly ILogger _logger;
    private readonly IEventStore _eventStore;
    private readonly IEventBus _eventBus;

    protected DomainEventHandlerBase(ILogger logger, IEventStore eventStore, IEventBus eventBus)
    {
        _logger = logger;
        _eventStore = eventStore;
        _eventBus = eventBus;
    }

    public async Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Domain event published: {EventType} (EventId={EventId}, OccurredAt={OccurredAt}) {@Payload}",
            notification.EventType, notification.EventId, notification.OccurredAt, notification);

        try
        {
            await _eventStore.AppendAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to append domain event {EventType} (EventId={EventId}) to the event store.",
                notification.EventType, notification.EventId);
        }

        try
        {
            await _eventBus.PublishAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish domain event {EventType} (EventId={EventId}) to the message bus.",
                notification.EventType, notification.EventId);
        }
    }
}
