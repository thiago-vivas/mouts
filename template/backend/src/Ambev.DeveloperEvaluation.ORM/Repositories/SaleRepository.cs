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

        // Case-insensitive partial match. ToLower().Contains() translates to SQL
        // (lower(...) LIKE '%..%') on relational providers and runs in-memory too.
        if (!string.IsNullOrWhiteSpace(options.CustomerName))
        {
            var customer = options.CustomerName.ToLower();
            query = query.Where(s => s.Customer.Name.ToLower().Contains(customer));
        }

        if (!string.IsNullOrWhiteSpace(options.BranchName))
        {
            var branch = options.BranchName.ToLower();
            query = query.Where(s => s.Branch.Name.ToLower().Contains(branch));
        }

        if (options.IsCancelled.HasValue)
            query = query.Where(s => s.IsCancelled == options.IsCancelled.Value);

        if (options.MinTotalAmount.HasValue)
            query = query.Where(s => s.TotalAmount >= options.MinTotalAmount.Value);

        if (options.MaxTotalAmount.HasValue)
            query = query.Where(s => s.TotalAmount <= options.MaxTotalAmount.Value);

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
