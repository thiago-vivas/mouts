using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Raised when a new sale is created.</summary>
public class SaleCreatedEvent : DomainEvent
{
    public override string EventType => "SaleCreated";

    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public decimal TotalAmount { get; }

    public SaleCreatedEvent(Sale sale)
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
        TotalAmount = sale.TotalAmount;
    }
}
