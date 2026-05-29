using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Raised when an existing sale is updated.</summary>
public class SaleModifiedEvent : DomainEvent
{
    public override string EventType => "SaleModified";

    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public decimal TotalAmount { get; }

    public SaleModifiedEvent(Sale sale)
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
        TotalAmount = sale.TotalAmount;
    }
}
