using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Base class for sale domain events. Implements MediatR's <see cref="INotification"/>
/// so events can be published in-process and handled by logging / event-store handlers.
/// </summary>
public abstract class DomainEvent : INotification
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>Machine-readable event type name (e.g. "SaleCreated").</summary>
    public abstract string EventType { get; }
}
