using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSales;

/// <summary>
/// Query parameters for listing sales, following the conventions in
/// .doc/general-api.md (_page, _size, _order, field filters, _min/_max ranges).
/// </summary>
public class GetSalesRequest
{
    [FromQuery(Name = "_page")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "_size")]
    public int Size { get; set; } = 10;

    [FromQuery(Name = "_order")]
    public string? Order { get; set; }

    [FromQuery(Name = "customerName")]
    public string? CustomerName { get; set; }

    [FromQuery(Name = "branchName")]
    public string? BranchName { get; set; }

    [FromQuery(Name = "isCancelled")]
    public bool? IsCancelled { get; set; }

    [FromQuery(Name = "_minTotalAmount")]
    public decimal? MinTotalAmount { get; set; }

    [FromQuery(Name = "_maxTotalAmount")]
    public decimal? MaxTotalAmount { get; set; }

    [FromQuery(Name = "_minSaleDate")]
    public DateTime? MinSaleDate { get; set; }

    [FromQuery(Name = "_maxSaleDate")]
    public DateTime? MaxSaleDate { get; set; }
}
