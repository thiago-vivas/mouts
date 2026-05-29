using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Events;

public class SaleEventsTests
{
    [Fact(DisplayName = "Each event exposes its event type and metadata")]
    public void Given_Events_Then_HaveTypeAndMetadata()
    {
        // Arrange
        var sale = SaleTestData.GenerateValidSale();
        var item = sale.Items.First();

        // Act
        var created = new SaleCreatedEvent(sale);
        var modified = new SaleModifiedEvent(sale);
        var cancelled = new SaleCancelledEvent(sale);
        var itemCancelled = new ItemCancelledEvent(sale, item);

        // Assert
        created.EventType.Should().Be("SaleCreated");
        modified.EventType.Should().Be("SaleModified");
        cancelled.EventType.Should().Be("SaleCancelled");
        itemCancelled.EventType.Should().Be("ItemCancelled");

        created.SaleId.Should().Be(sale.Id);
        created.SaleNumber.Should().Be(sale.SaleNumber);
        created.TotalAmount.Should().Be(sale.TotalAmount);
        created.EventId.Should().NotBe(Guid.Empty);

        itemCancelled.ItemId.Should().Be(item.Id);
        itemCancelled.ProductName.Should().Be(item.Product.Name);
    }
}
