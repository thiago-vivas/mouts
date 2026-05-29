using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Validation;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Aggregate root for a sale record. Owns its <see cref="SaleItem"/> collection and
/// keeps the sale total consistent. All discount/quantity rules are enforced
/// through the items, so business logic stays in the domain. Descriptive state is
/// mutated only through the constructor and intent-revealing methods so the
/// aggregate can never be left in an inconsistent state from the outside.
/// </summary>
public class Sale : BaseEntity
{
    /// <summary>Unique, server-generated sale number.</summary>
    public string SaleNumber { get; private set; } = string.Empty;

    /// <summary>Date when the sale was made.</summary>
    public DateTime SaleDate { get; private set; }

    /// <summary>External Identity of the customer (id + denormalized name).</summary>
    public ExternalReference Customer { get; private set; } = null!;

    /// <summary>External Identity of the branch where the sale was made.</summary>
    public ExternalReference Branch { get; private set; } = null!;

    /// <summary>Total sale amount, computed from the active (non-cancelled) items.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Whether the whole sale has been cancelled.</summary>
    public bool IsCancelled { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<SaleItem> _items = new();
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    /// <summary>Parameterless constructor for EF Core materialization.</summary>
    private Sale()
    {
    }

    public Sale(string saleNumber, DateTime saleDate, ExternalReference customer, ExternalReference branch)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        SaleNumber = saleNumber;
        SaleDate = saleDate;
        Customer = customer;
        Branch = branch;
    }

    /// <summary>Updates the descriptive details of the sale (date, customer, branch).</summary>
    public void UpdateDetails(DateTime saleDate, ExternalReference customer, ExternalReference branch)
    {
        if (IsCancelled)
            throw new DomainException($"Sale {Id} is cancelled and cannot be modified.");

        SaleDate = saleDate;
        Customer = customer;
        Branch = branch;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Adds a new line item and recalculates the sale total.</summary>
    public SaleItem AddItem(ExternalReference product, int quantity, decimal unitPrice)
    {
        var item = new SaleItem(product, quantity, unitPrice) { SaleId = Id };
        _items.Add(item);
        Recalculate();
        return item;
    }

    /// <summary>
    /// Reconciles the sale's active items with the supplied set, matching by product
    /// identity: existing active lines are updated in place (preserving their id and
    /// history), products not yet present are added, and active lines no longer present
    /// are removed. Already-cancelled lines are preserved untouched.
    /// </summary>
    public void SyncItems(IEnumerable<(ExternalReference Product, int Quantity, decimal UnitPrice)> desiredItems)
    {
        if (IsCancelled)
            throw new DomainException($"Sale {Id} is cancelled and cannot be modified.");

        var desired = desiredItems.ToList();
        var desiredProductIds = desired.Select(d => d.Product.Id).ToHashSet();

        var staleActive = _items
            .Where(i => !i.IsCancelled && !desiredProductIds.Contains(i.Product.Id))
            .ToList();
        foreach (var stale in staleActive)
            _items.Remove(stale);

        foreach (var desiredItem in desired)
        {
            var existing = _items.FirstOrDefault(i => !i.IsCancelled && i.Product.Id == desiredItem.Product.Id);
            if (existing is not null)
                existing.Update(desiredItem.Product, desiredItem.Quantity, desiredItem.UnitPrice);
            else
                _items.Add(new SaleItem(desiredItem.Product, desiredItem.Quantity, desiredItem.UnitPrice) { SaleId = Id });
        }

        UpdatedAt = DateTime.UtcNow;
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

    /// <summary>
    /// Cancels the whole sale: every active line item is cancelled and the total
    /// collapses to zero, keeping the aggregate internally consistent.
    /// </summary>
    public void Cancel()
    {
        if (IsCancelled)
            throw new DomainException($"Sale {Id} is already cancelled.");

        foreach (var item in _items.Where(i => !i.IsCancelled))
            item.Cancel();

        IsCancelled = true;
        UpdatedAt = DateTime.UtcNow;
        Recalculate();
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
