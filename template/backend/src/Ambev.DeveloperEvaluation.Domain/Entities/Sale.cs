using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Validation;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Aggregate root for a sale record. Owns its <see cref="SaleItem"/> collection and
/// keeps the sale total consistent. All discount/quantity rules are enforced
/// through the items, so business logic stays in the domain.
/// </summary>
public class Sale : BaseEntity
{
    /// <summary>Unique, server-generated sale number.</summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>Date when the sale was made.</summary>
    public DateTime SaleDate { get; set; }

    /// <summary>External Identity of the customer (id + denormalized name).</summary>
    public ExternalReference Customer { get; set; } = null!;

    /// <summary>External Identity of the branch where the sale was made.</summary>
    public ExternalReference Branch { get; set; } = null!;

    /// <summary>Total sale amount, computed from the active (non-cancelled) items.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Whether the whole sale has been cancelled.</summary>
    public bool IsCancelled { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    private readonly List<SaleItem> _items = new();
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    public Sale()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Adds a new line item and recalculates the sale total.</summary>
    public SaleItem AddItem(ExternalReference product, int quantity, decimal unitPrice)
    {
        var item = new SaleItem(product, quantity, unitPrice) { SaleId = Id };
        _items.Add(item);
        Recalculate();
        return item;
    }

    /// <summary>Removes all items (used when fully replacing a sale's items on update).</summary>
    public void ClearItems()
    {
        _items.Clear();
        Recalculate();
    }

    /// <summary>Cancels a single item by id and recalculates the total.</summary>
    public SaleItem CancelItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
                   ?? throw new DomainException($"Item {itemId} not found in sale {Id}.");

        if (item.IsCancelled)
            throw new DomainException($"Item {itemId} is already cancelled.");

        item.Cancel();
        Recalculate();
        return item;
    }

    /// <summary>Cancels the whole sale.</summary>
    public void Cancel()
    {
        if (IsCancelled)
            throw new DomainException($"Sale {Id} is already cancelled.");

        IsCancelled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Recomputes <see cref="TotalAmount"/> as the sum of active item totals.</summary>
    public void Recalculate()
    {
        TotalAmount = _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);
    }

    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }
}
