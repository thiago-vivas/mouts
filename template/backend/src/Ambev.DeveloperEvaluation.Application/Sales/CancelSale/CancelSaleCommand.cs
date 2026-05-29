using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

/// <summary>Command to cancel an entire sale.</summary>
public class CancelSaleCommand : IRequest<SaleResult>
{
    public Guid Id { get; set; }

    public CancelSaleCommand(Guid id) => Id = id;
}
