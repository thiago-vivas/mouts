using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Raised when a whole sale is cancelled.</summary>
public class SaleCancelledEvent : DomainEvent
{
    public override string EventType => "SaleCancelled";

    public Guid SaleId { get; }
    public string SaleNumber { get; }

    public SaleCancelledEvent(Sale sale)
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
    }
}
