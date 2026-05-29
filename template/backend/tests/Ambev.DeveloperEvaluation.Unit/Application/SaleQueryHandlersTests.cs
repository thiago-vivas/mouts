using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class GetSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly GetSaleHandler _handler;

    public GetSaleHandlerTests()
        => _handler = new GetSaleHandler(_repository, TestData.SalesMapper.Create());

    [Fact(DisplayName = "GetSale returns the mapped sale when found")]
    public async Task Given_ExistingSale_When_Handle_Then_ReturnsResult()
    {
        var sale = SaleTestData.GenerateValidSale();
        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(new GetSaleCommand(sale.Id), CancellationToken.None);

        result.Id.Should().Be(sale.Id);
        result.Items.Should().HaveCount(sale.Items.Count);
    }

    [Fact(DisplayName = "GetSale throws when the sale does not exist")]
    public async Task Given_MissingSale_When_Handle_Then_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(new GetSaleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class GetSalesHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly GetSalesHandler _handler;

    public GetSalesHandlerTests()
        => _handler = new GetSalesHandler(_repository, TestData.SalesMapper.Create());

    [Fact(DisplayName = "GetSales maps items and computes pagination metadata")]
    public async Task Given_Sales_When_Handle_Then_ReturnsPaginatedResult()
    {
        var sales = new List<Sale> { SaleTestData.GenerateValidSale(), SaleTestData.GenerateValidSale() };
        _repository.ListAsync(Arg.Any<SaleQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns((sales, 25));

        var result = await _handler.Handle(new GetSalesQuery { Page = 2, Size = 10 }, CancellationToken.None);

        result.Sales.Should().HaveCount(2);
        result.TotalCount.Should().Be(25);
        result.CurrentPage.Should().Be(2);
        result.TotalPages.Should().Be(3); // ceil(25/10)
    }

    [Fact(DisplayName = "GetSales forwards filters to the repository")]
    public async Task Given_Query_When_Handle_Then_PassesOptions()
    {
        _repository.ListAsync(Arg.Any<SaleQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns((new List<Sale>(), 0));

        await _handler.Handle(new GetSalesQuery { CustomerName = "john", IsCancelled = true }, CancellationToken.None);

        await _repository.Received(1).ListAsync(
            Arg.Is<SaleQueryOptions>(o => o.CustomerName == "john" && o.IsCancelled == true),
            Arg.Any<CancellationToken>());
    }
}
