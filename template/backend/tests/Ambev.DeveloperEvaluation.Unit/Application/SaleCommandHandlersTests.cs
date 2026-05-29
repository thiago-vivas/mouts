using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly UpdateSaleHandler _handler;

    public UpdateSaleHandlerTests()
    {
        _handler = new UpdateSaleHandler(_repository, TestData.SalesMapper.Create(), _mediator);
        _repository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Sale>());
    }

    private static UpdateSaleCommand ValidCommand(Guid id) => new()
    {
        Id = id,
        SaleDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Customer = new ExternalReferenceDto { Id = Guid.NewGuid(), Name = "New Customer" },
        Branch = new ExternalReferenceDto { Id = Guid.NewGuid(), Name = "New Branch" },
        Items = new List<UpdateSaleItemDto>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "P", Quantity = 12, UnitPrice = 3m }
        }
    };

    [Fact(DisplayName = "UpdateSale replaces items, recomputes total and publishes SaleModified")]
    public async Task Given_ExistingSale_When_Handle_Then_UpdatesAndPublishes()
    {
        var sale = SaleTestData.GenerateValidSale();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(ValidCommand(sale.Id), CancellationToken.None);

        result.Customer.Name.Should().Be("New Customer");
        result.Items.Should().HaveCount(1);
        result.TotalAmount.Should().Be(28.8m); // 12 * 3 - 20%
        await _mediator.Received(1).Publish(Arg.Any<SaleModifiedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "UpdateSale throws when the sale does not exist")]
    public async Task Given_MissingSale_When_Handle_Then_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateSale rejects an invalid command")]
    public async Task Given_InvalidCommand_When_Handle_Then_ThrowsValidation()
    {
        var command = ValidCommand(Guid.NewGuid());
        command.Items.Clear();

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}

public class DeleteSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly DeleteSaleHandler _handler;

    public DeleteSaleHandlerTests() => _handler = new DeleteSaleHandler(_repository);

    [Fact(DisplayName = "DeleteSale returns true when the repository deletes")]
    public async Task Given_ExistingSale_When_Handle_Then_ReturnsTrue()
    {
        _repository.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteSaleCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "DeleteSale throws when nothing was deleted")]
    public async Task Given_MissingSale_When_Handle_Then_Throws()
    {
        _repository.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _handler.Handle(new DeleteSaleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CancelSaleHandler _handler;

    public CancelSaleHandlerTests()
    {
        _handler = new CancelSaleHandler(_repository, TestData.SalesMapper.Create(), _mediator);
        _repository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Sale>());
    }

    [Fact(DisplayName = "CancelSale cancels and publishes SaleCancelled")]
    public async Task Given_ExistingSale_When_Handle_Then_CancelsAndPublishes()
    {
        var sale = SaleTestData.GenerateValidSale();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        result.IsCancelled.Should().BeTrue();
        await _mediator.Received(1).Publish(Arg.Any<SaleCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CancelSale throws when the sale does not exist")]
    public async Task Given_MissingSale_When_Handle_Then_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(new CancelSaleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class CancelSaleItemHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CancelSaleItemHandler _handler;

    public CancelSaleItemHandlerTests()
    {
        _handler = new CancelSaleItemHandler(_repository, TestData.SalesMapper.Create(), _mediator);
        _repository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Sale>());
    }

    [Fact(DisplayName = "CancelSaleItem cancels the item and publishes ItemCancelled")]
    public async Task Given_ExistingItem_When_Handle_Then_CancelsAndPublishes()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var itemId = sale.Items.First().Id;
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, itemId), CancellationToken.None);

        result.Items.Single(i => i.Id == itemId).IsCancelled.Should().BeTrue();
        await _mediator.Received(1).Publish(Arg.Any<ItemCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CancelSaleItem throws when the sale does not exist")]
    public async Task Given_MissingSale_When_Handle_Then_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(new CancelSaleItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "CancelSaleItem throws when the item is not in the sale")]
    public async Task Given_UnknownItem_When_Handle_Then_Throws()
    {
        var sale = SaleTestData.GenerateValidSale();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<Ambev.DeveloperEvaluation.Domain.Exceptions.DomainException>();
    }
}
