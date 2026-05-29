using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public UpdateSaleHandler(ISaleRepository saleRepository, IMapper mapper, IMediator mediator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<SaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        sale.SaleDate = command.SaleDate;
        sale.Customer = new ExternalReference(command.Customer.Id, command.Customer.Name);
        sale.Branch = new ExternalReference(command.Branch.Id, command.Branch.Name);

        // Full replacement of items; the domain recomputes discounts and totals.
        sale.ClearItems();
        foreach (var item in command.Items)
            sale.AddItem(new ExternalReference(item.ProductId, item.ProductName), item.Quantity, item.UnitPrice);

        sale.UpdatedAt = DateTime.UtcNow;

        var updated = await _saleRepository.UpdateAsync(sale, cancellationToken);

        await _mediator.Publish(new SaleModifiedEvent(updated), cancellationToken);

        return _mapper.Map<SaleResult>(updated);
    }
}
