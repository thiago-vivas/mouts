using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>Query options for listing sales (pagination + ordering).</summary>
public class SaleQueryOptions
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;

    /// <summary>Raw ordering expression, e.g. "saleDate desc, saleNumber asc".</summary>
    public string? Order { get; set; }

    /// <summary>Optional partial-match filter on the customer name.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Optional partial-match filter on the branch name.</summary>
    public string? BranchName { get; set; }

    /// <summary>Optional cancelled flag filter.</summary>
    public bool? IsCancelled { get; set; }

    public decimal? MinTotalAmount { get; set; }
    public decimal? MaxTotalAmount { get; set; }
}

/// <summary>Repository abstraction for the <see cref="Sale"/> aggregate.</summary>
public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a page of sales plus the total matching count.</summary>
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        SaleQueryOptions options, CancellationToken cancellationToken = default);
}
