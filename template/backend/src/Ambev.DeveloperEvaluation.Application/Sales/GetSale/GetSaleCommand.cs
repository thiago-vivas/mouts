using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>Query to retrieve a single sale by id.</summary>
public class GetSaleCommand : IRequest<SaleResult>
{
    public Guid Id { get; set; }

    public GetSaleCommand(Guid id) => Id = id;
}
