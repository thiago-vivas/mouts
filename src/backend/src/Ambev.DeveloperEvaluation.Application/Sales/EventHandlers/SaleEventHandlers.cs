using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

public class SaleCreatedEventHandler : DomainEventHandlerBase<SaleCreatedEvent>
{
    public SaleCreatedEventHandler(ILogger<SaleCreatedEventHandler> logger, IEventStore eventStore, IEventBus eventBus)
        : base(logger, eventStore, eventBus) { }
}

public class SaleModifiedEventHandler : DomainEventHandlerBase<SaleModifiedEvent>
{
    public SaleModifiedEventHandler(ILogger<SaleModifiedEventHandler> logger, IEventStore eventStore, IEventBus eventBus)
        : base(logger, eventStore, eventBus) { }
}

public class SaleCancelledEventHandler : DomainEventHandlerBase<SaleCancelledEvent>
{
    public SaleCancelledEventHandler(ILogger<SaleCancelledEventHandler> logger, IEventStore eventStore, IEventBus eventBus)
        : base(logger, eventStore, eventBus) { }
}

public class ItemCancelledEventHandler : DomainEventHandlerBase<ItemCancelledEvent>
{
    public ItemCancelledEventHandler(ILogger<ItemCancelledEventHandler> logger, IEventStore eventStore, IEventBus eventBus)
        : base(logger, eventStore, eventBus) { }
}
