using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public CreateSaleHandler(ISaleRepository saleRepository, IMapper mapper, IMediator mediator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = new Sale(
            SaleNumberGenerator.Next(),
            command.SaleDate,
            new ExternalReference(command.Customer.Id, command.Customer.Name),
            new ExternalReference(command.Branch.Id, command.Branch.Name));

        // AddItem applies the discount policy (and rejects qty > 20) per item.
        foreach (var item in command.Items)
            sale.AddItem(new ExternalReference(item.ProductId, item.ProductName), item.Quantity, item.UnitPrice);

        var domainValidation = sale.Validate();
        if (!domainValidation.IsValid)
            throw new ValidationException(domainValidation.Errors.Select(e =>
                new FluentValidation.Results.ValidationFailure(e.Error, e.Detail)));

        var created = await _saleRepository.CreateAsync(sale, cancellationToken);

        await _mediator.Publish(new SaleCreatedEvent(created), cancellationToken);

        return _mapper.Map<SaleResult>(created);
    }
}
