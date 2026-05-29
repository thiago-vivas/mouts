using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Command to update a sale. Replaces the customer/branch/date and the full set
/// of items (totals are recomputed from the new items by the domain).
/// </summary>
public class UpdateSaleCommand : IRequest<SaleResult>
{
    public Guid Id { get; set; }
    public DateTime SaleDate { get; set; }
    public ExternalReferenceDto Customer { get; set; } = new();
    public ExternalReferenceDto Branch { get; set; } = new();
    public List<UpdateSaleItemDto> Items { get; set; } = new();
}

public class UpdateSaleItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
