using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// A line item within a <see cref="Sale"/>. Holds the external product reference,
/// quantity and unit price, and derives its discount and total from the
/// <see cref="DiscountPolicy"/>.
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>FK back to the owning sale.</summary>
    public Guid SaleId { get; set; }

    /// <summary>External Identity of the product (id + denormalized name).</summary>
    public ExternalReference Product { get; private set; } = null!;

    /// <summary>Number of identical units. Constrained to 1..20 by the discount policy.</summary>
    public int Quantity { get; private set; }

    /// <summary>Unit price of the product at sale time.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Monetary discount applied to this line (computed from the quantity tier).</summary>
    public decimal Discount { get; private set; }

    /// <summary>Net total for this line: (UnitPrice * Quantity) - Discount.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Whether this individual item has been cancelled.</summary>
    public bool IsCancelled { get; private set; }

    private SaleItem() { }

    public SaleItem(ExternalReference product, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        Product = product;
        UnitPrice = unitPrice;
        SetQuantity(quantity);
    }

    /// <summary>
    /// Sets the quantity and recomputes discount/total by applying the discount policy.
    /// Throws <see cref="Exceptions.DomainException"/> if the quantity breaks the rules.
    /// </summary>
    public void SetQuantity(int quantity)
    {
        var rate = DiscountPolicy.GetDiscountRate(quantity);
        Quantity = quantity;
        var gross = UnitPrice * quantity;
        Discount = decimal.Round(gross * rate, 2);
        TotalAmount = gross - Discount;
    }

    /// <summary>Updates the unit price and recomputes the totals.</summary>
    public void SetUnitPrice(decimal unitPrice)
    {
        UnitPrice = unitPrice;
        SetQuantity(Quantity);
    }

    /// <summary>
    /// Updates the product, unit price and quantity of an existing line in place,
    /// preserving the line's identity, and recomputes discount/total.
    /// </summary>
    public void Update(ExternalReference product, int quantity, decimal unitPrice)
    {
        Product = product;
        UnitPrice = unitPrice;
        SetQuantity(quantity);
    }

    /// <summary>Cancels this item; its total no longer counts toward the sale total.</summary>
    public void Cancel() => IsCancelled = true;
}
