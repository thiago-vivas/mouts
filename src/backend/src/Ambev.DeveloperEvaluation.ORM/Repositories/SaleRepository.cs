using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>EF Core implementation of <see cref="ISaleRepository"/>.</summary>
public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        // Callers load the aggregate via GetByIdAsync (tracked) and mutate it, so
        // the change tracker already holds the correct per-entity states
        // (added/removed items, modified scalars). Only a detached aggregate needs
        // an explicit Update — calling Update() on a tracked graph would wrongly
        // mark newly-added items as Modified.
        if (_context.Entry(sale).State == EntityState.Detached)
            _context.Sales.Update(sale);

        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        SaleQueryOptions options, CancellationToken cancellationToken = default)
    {
        // Read-only listing: disable change tracking for a lighter, faster query.
        var query = _context.Sales.AsNoTracking().Include(s => s.Items).AsQueryable();

        // String filters follow .doc/general-api.md: a leading/trailing '*' means a
        // partial match, otherwise it is an exact (case-insensitive) match.
        if (!string.IsNullOrWhiteSpace(options.CustomerName))
        {
            var (kind, value) = ParseWildcard(options.CustomerName);
            query = kind switch
            {
                NameMatch.Contains => query.Where(s => s.Customer.Name.ToLower().Contains(value)),
                NameMatch.StartsWith => query.Where(s => s.Customer.Name.ToLower().StartsWith(value)),
                NameMatch.EndsWith => query.Where(s => s.Customer.Name.ToLower().EndsWith(value)),
                _ => query.Where(s => s.Customer.Name.ToLower() == value)
            };
        }

        if (!string.IsNullOrWhiteSpace(options.BranchName))
        {
            var (kind, value) = ParseWildcard(options.BranchName);
            query = kind switch
            {
                NameMatch.Contains => query.Where(s => s.Branch.Name.ToLower().Contains(value)),
                NameMatch.StartsWith => query.Where(s => s.Branch.Name.ToLower().StartsWith(value)),
                NameMatch.EndsWith => query.Where(s => s.Branch.Name.ToLower().EndsWith(value)),
                _ => query.Where(s => s.Branch.Name.ToLower() == value)
            };
        }

        if (options.IsCancelled.HasValue)
            query = query.Where(s => s.IsCancelled == options.IsCancelled.Value);

        if (options.MinTotalAmount.HasValue)
            query = query.Where(s => s.TotalAmount >= options.MinTotalAmount.Value);

        if (options.MaxTotalAmount.HasValue)
            query = query.Where(s => s.TotalAmount <= options.MaxTotalAmount.Value);

        // Dates from the query string are DateTimeKind.Unspecified; treat them as UTC
        // so the comparison is valid against the timestamptz column (Npgsql requires it).
        if (options.MinSaleDate.HasValue)
        {
            var min = DateTime.SpecifyKind(options.MinSaleDate.Value, DateTimeKind.Utc);
            query = query.Where(s => s.SaleDate >= min);
        }

        if (options.MaxSaleDate.HasValue)
        {
            var max = DateTime.SpecifyKind(options.MaxSaleDate.Value, DateTimeKind.Utc);
            query = query.Where(s => s.SaleDate <= max);
        }

        query = ApplyOrdering(query, options.Order);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, options.Page);
        var size = options.Size <= 0 ? 10 : options.Size;

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private enum NameMatch { Exact, StartsWith, EndsWith, Contains }

    /// <summary>
    /// Parses a name filter into a match kind + lowered comparison value. A leading
    /// and/or trailing '*' denotes a partial match (per .doc/general-api.md). ToLower
    /// is used (rather than <c>EF.Functions.ILike</c>) so the same query translates on
    /// both PostgreSQL and the EF in-memory provider the test suite runs against.
    /// </summary>
    private static (NameMatch Kind, string Value) ParseWildcard(string raw)
    {
        var value = raw.Trim('*').ToLower();
        var prefixStar = raw.StartsWith("*");
        var suffixStar = raw.EndsWith("*");

        return (prefixStar, suffixStar) switch
        {
            (true, true) => (NameMatch.Contains, value),
            (false, true) => (NameMatch.StartsWith, value),
            (true, false) => (NameMatch.EndsWith, value),
            _ => (NameMatch.Exact, value)
        };
    }

    /// <summary>
    /// Applies an ordering expression like "saleDate desc, saleNumber asc".
    /// Defaults to most-recent-first when no/invalid order is provided.
    /// </summary>
    private static IQueryable<Sale> ApplyOrdering(IQueryable<Sale> query, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return query.OrderByDescending(s => s.SaleDate);

        IOrderedQueryable<Sale>? ordered = null;

        foreach (var raw in order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = parts[0].Trim('"').ToLowerInvariant();
            var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            ordered = (field, desc, ordered) switch
            {
                ("salenumber", false, null) => query.OrderBy(s => s.SaleNumber),
                ("salenumber", true, null) => query.OrderByDescending(s => s.SaleNumber),
                ("salenumber", false, _) => ordered!.ThenBy(s => s.SaleNumber),
                ("salenumber", true, _) => ordered!.ThenByDescending(s => s.SaleNumber),

                ("totalamount", false, null) => query.OrderBy(s => s.TotalAmount),
                ("totalamount", true, null) => query.OrderByDescending(s => s.TotalAmount),
                ("totalamount", false, _) => ordered!.ThenBy(s => s.TotalAmount),
                ("totalamount", true, _) => ordered!.ThenByDescending(s => s.TotalAmount),

                ("saledate", true, null) => query.OrderByDescending(s => s.SaleDate),
                ("saledate", false, null) => query.OrderBy(s => s.SaleDate),
                ("saledate", true, _) => ordered!.ThenByDescending(s => s.SaleDate),
                ("saledate", false, _) => ordered!.ThenBy(s => s.SaleDate),

                _ => ordered
            };
        }

        return ordered ?? query.OrderByDescending(s => s.SaleDate);
    }
}
