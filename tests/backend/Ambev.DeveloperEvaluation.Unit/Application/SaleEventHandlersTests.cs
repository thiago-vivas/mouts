using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Verifies the domain-event notification handlers append to the event store
/// (MongoDB audit log) and publish to the event bus (Rebus/RabbitMQ).
/// </summary>
public class SaleEventHandlersTests
{
    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    [Fact(DisplayName = "SaleCreated handler appends to the store and publishes to the bus")]
    public async Task SaleCreatedHandler_AppendsAndPublishes()
    {
        // Arrange
        var handler = new SaleCreatedEventHandler(Substitute.For<ILogger<SaleCreatedEventHandler>>(), _eventStore, _eventBus);
        var evt = new SaleCreatedEvent(SaleTestData.GenerateValidSale());

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        await _eventStore.Received(1).AppendAsync(evt, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ItemCancelled handler appends to the store and publishes to the bus")]
    public async Task ItemCancelledHandler_AppendsAndPublishes()
    {
        // Arrange
        var sale = SaleTestData.GenerateValidSale();
        var handler = new ItemCancelledEventHandler(Substitute.For<ILogger<ItemCancelledEventHandler>>(), _eventStore, _eventBus);
        var evt = new ItemCancelledEvent(sale, sale.Items.First());

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        await _eventStore.Received(1).AppendAsync(evt, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "SaleModified handler appends to the store and publishes to the bus")]
    public async Task SaleModifiedHandler_AppendsAndPublishes()
    {
        // Arrange
        var handler = new SaleModifiedEventHandler(Substitute.For<ILogger<SaleModifiedEventHandler>>(), _eventStore, _eventBus);
        var evt = new SaleModifiedEvent(SaleTestData.GenerateValidSale());

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        await _eventStore.Received(1).AppendAsync(evt, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "SaleCancelled handler appends to the store and publishes to the bus")]
    public async Task SaleCancelledHandler_AppendsAndPublishes()
    {
        // Arrange
        var handler = new SaleCancelledEventHandler(Substitute.For<ILogger<SaleCancelledEventHandler>>(), _eventStore, _eventBus);
        var evt = new SaleCancelledEvent(SaleTestData.GenerateValidSale());

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        await _eventStore.Received(1).AppendAsync(evt, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A failing event store does not fail the handler and the bus is still tried")]
    public async Task Handler_WhenStoreThrows_DoesNotThrow_AndStillPublishes()
    {
        // Arrange
        _eventStore.AppendAsync(Arg.Any<DomainEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("event store unavailable"));
        var handler = new SaleCreatedEventHandler(Substitute.For<ILogger<SaleCreatedEventHandler>>(), _eventStore, _eventBus);
        var evt = new SaleCreatedEvent(SaleTestData.GenerateValidSale());

        // Act
        var act = () => handler.Handle(evt, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        await _eventBus.Received(1).PublishAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A failing event bus does not fail the handler")]
    public async Task Handler_WhenBusThrows_DoesNotThrow()
    {
        // Arrange
        _eventBus.PublishAsync(Arg.Any<DomainEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("broker unavailable"));
        var handler = new SaleCreatedEventHandler(Substitute.For<ILogger<SaleCreatedEventHandler>>(), _eventStore, _eventBus);
        var evt = new SaleCreatedEvent(SaleTestData.GenerateValidSale());

        // Act
        var act = () => handler.Handle(evt, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        await _eventStore.Received(1).AppendAsync(evt, Arg.Any<CancellationToken>());
    }
}
