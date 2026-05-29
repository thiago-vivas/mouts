using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Unit tests for <see cref="CreateSaleHandler"/> using NSubstitute mocks for the
/// repository and mediator, and a real AutoMapper configuration.
/// </summary>
public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper;
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SaleResultProfile>());
        _mapper = config.CreateMapper();
        _handler = new CreateSaleHandler(_saleRepository, _mapper, _mediator);

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sale>());
    }

    [Fact(DisplayName = "Creating a valid sale persists it and returns a result")]
    public async Task Given_ValidCommand_When_Handle_Then_PersistsAndReturnsResult()
    {
        var command = CreateSaleHandlerTestData.GenerateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.SaleNumber.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(command.Items.Count);
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Creating a sale publishes a SaleCreated event")]
    public async Task Given_ValidCommand_When_Handle_Then_PublishesSaleCreatedEvent()
    {
        var command = CreateSaleHandlerTestData.GenerateValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        await _mediator.Received(1).Publish(Arg.Any<SaleCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Creating a sale with no items throws validation error")]
    public async Task Given_CommandWithoutItems_When_Handle_Then_ThrowsValidation()
    {
        var command = CreateSaleHandlerTestData.GenerateValidCommand();
        command.Items.Clear();

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
