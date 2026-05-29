using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>Command to create a new sale. The sale number is generated server-side.</summary>
public class CreateSaleCommand : IRequest<SaleResult>
{
    public DateTime SaleDate { get; set; }
    public ExternalReferenceDto Customer { get; set; } = new();
    public ExternalReferenceDto Branch { get; set; } = new();
    public List<CreateSaleItemDto> Items { get; set; } = new();
}

/// <summary>An item supplied when creating/updating a sale.</summary>
public class CreateSaleItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
