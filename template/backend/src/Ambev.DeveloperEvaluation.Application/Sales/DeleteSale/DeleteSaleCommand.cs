using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>Command to permanently delete a sale.</summary>
public class DeleteSaleCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public DeleteSaleCommand(Guid id) => Id = id;
}
