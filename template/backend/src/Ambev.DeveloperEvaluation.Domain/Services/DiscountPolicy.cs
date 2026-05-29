using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Encapsulates the quantity-based discount business rules. This is the single
/// source of truth for discount tiers and limits — never duplicated in
/// controllers or handlers.
///
/// Rules:
///  - quantity &lt; 4        : no discount (0%)
///  - 4 &lt;= quantity &lt; 10 : 10% discount
///  - 10 &lt;= quantity &lt;= 20: 20% discount
///  - quantity &gt; 20       : not allowed (max 20 identical items per product)
/// </summary>
public static class DiscountPolicy
{
    public const int MaxQuantityPerItem = 20;
    private const int Tier10Threshold = 4;
    private const int Tier20Threshold = 10;

    /// <summary>
    /// Returns the discount rate (0, 0.10 or 0.20) for the given quantity.
    /// </summary>
    /// <exception cref="DomainException">When quantity is outside the allowed range.</exception>
    public static decimal GetDiscountRate(int quantity)
    {
        if (quantity < 1)
            throw new DomainException("Item quantity must be at least 1.");

        if (quantity > MaxQuantityPerItem)
            throw new DomainException(
                $"Cannot sell more than {MaxQuantityPerItem} identical items. Requested: {quantity}.");

        if (quantity >= Tier20Threshold)
            return 0.20m;

        if (quantity >= Tier10Threshold)
            return 0.10m;

        return 0m;
    }
}
