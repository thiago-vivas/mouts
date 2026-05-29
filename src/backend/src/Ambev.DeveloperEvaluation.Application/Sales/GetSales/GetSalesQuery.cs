using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSales;

/// <summary>Query to list sales with pagination, ordering and filtering.</summary>
public class GetSalesQuery : IRequest<GetSalesResult>
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? Order { get; set; }
    public string? CustomerName { get; set; }
    public string? BranchName { get; set; }
    public bool? IsCancelled { get; set; }
    public decimal? MinTotalAmount { get; set; }
    public decimal? MaxTotalAmount { get; set; }
    public DateTime? MinSaleDate { get; set; }
    public DateTime? MaxSaleDate { get; set; }
}

/// <summary>Paginated list result for sales.</summary>
public class GetSalesResult
{
    public List<SaleResult> Sales { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
