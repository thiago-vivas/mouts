namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Append-only store for published domain events. Implemented over MongoDB as an
/// immutable audit log / future read-model source.
/// </summary>
public interface IEventStore
{
    Task AppendAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
