using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Raised when a single item within a sale is cancelled.</summary>
public class ItemCancelledEvent : DomainEvent
{
    public override string EventType => "ItemCancelled";

    public Guid SaleId { get; }
    public Guid ItemId { get; }
    public string ProductName { get; }

    public ItemCancelledEvent(Sale sale, SaleItem item)
    {
        SaleId = sale.Id;
        ItemId = item.Id;
        ProductName = item.Product?.Name ?? string.Empty;
    }
}
