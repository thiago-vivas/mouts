using Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>Verifies the domain-event notification handlers append to the event store.</summary>
public class SaleEventHandlersTests
{
    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();

    [Fact(DisplayName = "SaleCreated handler appends the event to the store")]
    public async Task SaleCreatedHandler_Appends()
    {
        var handler = new SaleCreatedEventHandler(Substitute.For<ILogger<SaleCreatedEventHandler>>(), _eventStore);
        var evt = new SaleCreatedEvent(SaleTestData.GenerateValidSale());

        await handler.Handle(evt, CancellationToken.None);

        await _eventStore.Received(1).AppendAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ItemCancelled handler appends the event to the store")]
    public async Task ItemCancelledHandler_Appends()
    {
        var sale = SaleTestData.GenerateValidSale();
        var handler = new ItemCancelledEventHandler(Substitute.For<ILogger<ItemCancelledEventHandler>>(), _eventStore);
        var evt = new ItemCancelledEvent(sale, sale.Items.First());

        await handler.Handle(evt, CancellationToken.None);

        await _eventStore.Received(1).AppendAsync(evt, Arg.Any<CancellationToken>());
    }
}
