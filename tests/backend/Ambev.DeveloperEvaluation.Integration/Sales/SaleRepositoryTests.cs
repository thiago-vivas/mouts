using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

/// <summary>
/// Integration tests for <see cref="SaleRepository"/> exercising the EF Core
/// stack (DbContext, owned External-Identity mappings, Include, cascade delete,
/// pagination/ordering/filtering) end-to-end through the provider.
///
/// They run on the EF Core in-memory provider so the suite is hermetic (no Docker
/// required) in CI and locally. The exact same repository code runs against
/// PostgreSQL in production (configured in Program.cs / appsettings.json).
///
/// xUnit creates a fresh instance per test, so each test gets its own isolated
/// database via the unique <see cref="_databaseName"/>.
/// </summary>
public class SaleRepositoryTests
{
    private readonly string _databaseName = $"sales-{Guid.NewGuid()}";

    private DefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        return new DefaultContext(options);
    }

    private static Sale BuildSale(string number, string customer, string branch,
        bool cancel, params (int qty, decimal price)[] items)
    {
        var sale = new Sale(
            number,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new ExternalReference(Guid.NewGuid(), customer),
            new ExternalReference(Guid.NewGuid(), branch));
        foreach (var (qty, price) in items)
            sale.AddItem(new ExternalReference(Guid.NewGuid(), "Product"), qty, price);
        if (cancel) sale.Cancel();
        return sale;
    }

    [Fact(DisplayName = "Create then GetById round-trips the sale with computed totals")]
    public async Task Given_Sale_When_CreatedAndFetched_Then_PersistsWithTotals()
    {
        // Arrange
        var sale = BuildSale("S-INT-0001", "John Doe", "Downtown", false, (5, 10m));

        // Act
        await using (var ctx = CreateContext())
            await new SaleRepository(ctx).CreateAsync(sale);

        await using var verifyCtx = CreateContext();
        var loaded = await new SaleRepository(verifyCtx).GetByIdAsync(sale.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Items.Should().HaveCount(1);
        loaded.Customer.Name.Should().Be("John Doe");
        loaded.Items.First().Discount.Should().Be(5m);   // 10% of 50
        loaded.TotalAmount.Should().Be(45m);
    }

    [Fact(DisplayName = "Update replaces items and recomputes the total")]
    public async Task Given_Sale_When_Updated_Then_ItemsReplacedAndTotalRecomputed()
    {
        // Arrange
        var sale = BuildSale("S-INT-0002", "Mary", "Airport", false, (5, 10m));
        await using (var ctx = CreateContext())
            await new SaleRepository(ctx).CreateAsync(sale);

        // Act
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            var toUpdate = await repo.GetByIdAsync(sale.Id);
            // Replace the line with a different product (qty 12, $3 => 20% of 36 = 7.2).
            toUpdate!.SyncItems(new[]
            {
                (new ExternalReference(Guid.NewGuid(), "Product"), 12, 3m)
            });
            await repo.UpdateAsync(toUpdate);
        }

        await using var verifyCtx = CreateContext();
        var loaded = await new SaleRepository(verifyCtx).GetByIdAsync(sale.Id);

        // Assert
        loaded!.Items.Should().HaveCount(1);
        loaded.Items.First().Quantity.Should().Be(12);
        loaded.TotalAmount.Should().Be(28.8m);  // 36 - 7.2
    }

    [Fact(DisplayName = "Delete removes the sale and cascades to its items")]
    public async Task Given_Sale_When_Deleted_Then_GoneWithItems()
    {
        // Arrange
        var sale = BuildSale("S-INT-0003", "Acme", "Downtown", false, (4, 10m));
        await using (var ctx = CreateContext())
            await new SaleRepository(ctx).CreateAsync(sale);

        // Act
        bool deleted;
        await using (var ctx = CreateContext())
            deleted = await new SaleRepository(ctx).DeleteAsync(sale.Id);

        // Assert
        deleted.Should().BeTrue();

        await using var verifyCtx = CreateContext();
        (await new SaleRepository(verifyCtx).GetByIdAsync(sale.Id)).Should().BeNull();
        (await verifyCtx.SaleItems.CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "Delete returns false for an unknown id")]
    public async Task Given_UnknownId_When_Deleted_Then_ReturnsFalse()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act & Assert
        (await new SaleRepository(ctx).DeleteAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact(DisplayName = "List applies pagination and reports the full count")]
    public async Task Given_ManySales_When_Listed_Then_Paginated()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-1001", "A", "B", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-1002", "C", "D", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-1003", "E", "F", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { Page = 1, Size = 2 });

        // Assert
        items.Should().HaveCount(2);
        total.Should().Be(3);
    }

    [Fact(DisplayName = "List filters by customer name with a '*' wildcard (prefix match)")]
    public async Task Given_Sales_When_FilteredByCustomerWildcard_Then_PrefixMatch()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-2001", "Johnathan Smith", "B", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-2002", "Maria Garcia", "B", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { CustomerName = "johnathan*" }); // case-insensitive prefix

        // Assert
        total.Should().Be(1);
        items.Single().Customer.Name.Should().Be("Johnathan Smith");
    }

    [Fact(DisplayName = "List filters by customer name with an exact (no-wildcard) match")]
    public async Task Given_Sales_When_FilteredByCustomerExact_Then_OnlyExact()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-2201", "John", "B", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-2202", "Johnathan Smith", "B", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { CustomerName = "John" }); // exact, not partial

        // Assert
        total.Should().Be(1);
        items.Single().Customer.Name.Should().Be("John");
    }

    [Fact(DisplayName = "List filters by sale-date range")]
    public async Task Given_Sales_When_FilteredByDateRange_Then_OnlyInRange()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            var jan = BuildSale("S-INT-2301", "A", "B", false, (3, 10m)); // BuildSale stamps 2026-01-01
            var dec = BuildSale("S-INT-2302", "C", "D", false, (3, 10m));
            dec.UpdateDetails(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), dec.Customer, dec.Branch);
            await repo.CreateAsync(jan);
            await repo.CreateAsync(dec);
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { MaxSaleDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });

        // Assert
        total.Should().Be(1);
        items.Single().SaleNumber.Should().Be("S-INT-2301");
    }

    [Fact(DisplayName = "List filters by branch name and total amount range")]
    public async Task Given_Sales_When_FilteredByBranchAndRange_Then_Matches()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-2101", "A", "Downtown Store", false, (3, 10m)));   // total 30
            await repo.CreateAsync(BuildSale("S-INT-2102", "B", "Airport Store", false, (3, 100m)));   // total 300
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { BranchName = "airport*", MinTotalAmount = 100m });

        // Assert
        total.Should().Be(1);
        items.Single().Branch.Name.Should().Be("Airport Store");
    }

    [Fact(DisplayName = "List filters by cancelled flag")]
    public async Task Given_Sales_When_FilteredByCancelled_Then_OnlyCancelledReturned()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-3001", "A", "B", true, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-3002", "C", "D", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { IsCancelled = true });

        // Assert
        total.Should().Be(1);
        items.Single().IsCancelled.Should().BeTrue();
    }

    [Fact(DisplayName = "List orders by total amount descending")]
    public async Task Given_Sales_When_OrderedByTotalDesc_Then_HighestFirst()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-4001", "A", "B", false, (3, 10m)));   // 30
            await repo.CreateAsync(BuildSale("S-INT-4002", "C", "D", false, (3, 50m)));   // 150
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, _) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { Order = "totalAmount desc" });

        // Assert
        items.First().TotalAmount.Should().Be(150m);
    }

    [Theory(DisplayName = "List supports ordering by sale number and sale date in both directions")]
    [InlineData("saleNumber asc", "S-INT-5001")]
    [InlineData("saleNumber desc", "S-INT-5003")]
    [InlineData("saleDate asc", "S-INT-5001")]
    [InlineData("saleDate desc", "S-INT-5001")] // all same seeded date → stable, first inserted
    public async Task Given_Sales_When_OrderedByField_Then_FirstAsExpected(string order, string expectedFirstNumber)
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-5001", "A", "B", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-5002", "C", "D", false, (3, 20m)));
            await repo.CreateAsync(BuildSale("S-INT-5003", "E", "F", false, (3, 30m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, _) = await new SaleRepository(ctx2).ListAsync(new SaleQueryOptions { Order = order });

        // Assert
        items.First().SaleNumber.Should().Be(expectedFirstNumber);
    }

    [Theory(DisplayName = "List supports multi-field ordering across all secondary keys")]
    [InlineData("saleDate desc, saleNumber asc", "S-INT-6001")]
    [InlineData("saleDate asc, saleNumber desc", "S-INT-6002")]
    [InlineData("totalAmount asc, saleNumber asc", "S-INT-6001")]
    [InlineData("totalAmount desc, saleNumber desc", "S-INT-6002")]
    [InlineData("saleNumber asc, totalAmount desc", "S-INT-6001")]
    public async Task Given_Sales_When_MultiFieldOrder_Then_Applied(string order, string expectedFirst)
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            // Same date and total so the secondary key decides ordering.
            await repo.CreateAsync(BuildSale("S-INT-6002", "A", "B", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-6001", "C", "D", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, _) = await new SaleRepository(ctx2).ListAsync(new SaleQueryOptions { Order = order });

        // Assert
        items.First().SaleNumber.Should().Be(expectedFirst);
    }

    [Fact(DisplayName = "List ignores unknown order fields and still returns results")]
    public async Task Given_UnknownOrderField_When_Listed_Then_ReturnsAll()
    {
        // Arrange
        await using (var ctx = CreateContext())
            await new SaleRepository(ctx).CreateAsync(BuildSale("S-INT-7001", "A", "B", false, (3, 10m)));

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { Order = "unknownField desc" });

        // Assert
        total.Should().Be(1);
        items.Should().HaveCount(1);
    }

    [Fact(DisplayName = "List filters by customer name with a leading '*' (suffix match)")]
    public async Task Given_Sales_When_FilteredByCustomerSuffix_Then_SuffixMatch()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-2401", "Johnathan Smith", "B", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-2402", "Maria Garcia", "B", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { CustomerName = "*smith" });

        // Assert
        total.Should().Be(1);
        items.Single().Customer.Name.Should().Be("Johnathan Smith");
    }

    [Fact(DisplayName = "List filters by customer name with surrounding '*' (contains match)")]
    public async Task Given_Sales_When_FilteredByCustomerContains_Then_ContainsMatch()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            await repo.CreateAsync(BuildSale("S-INT-2501", "Johnathan Smith", "B", false, (3, 10m)));
            await repo.CreateAsync(BuildSale("S-INT-2502", "Maria Garcia", "B", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { CustomerName = "*nathan*" });

        // Assert
        total.Should().Be(1);
        items.Single().Customer.Name.Should().Be("Johnathan Smith");
    }

    [Fact(DisplayName = "List falls back to the default page size when size <= 0")]
    public async Task Given_NonPositiveSize_When_Listed_Then_DefaultsToTen()
    {
        // Arrange
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            for (var i = 1; i <= 3; i++)
                await repo.CreateAsync(BuildSale($"S-INT-8{i:000}", "A", "B", false, (3, 10m)));
        }

        // Act
        await using var ctx2 = CreateContext();
        var (items, total) = await new SaleRepository(ctx2)
            .ListAsync(new SaleQueryOptions { Size = 0 });

        // Assert: size 0 must not return an empty page; default of 10 applies.
        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Update preserves item identity when the same product is kept")]
    public async Task Given_Sale_When_UpdatedWithSameProduct_Then_ItemIdPreserved()
    {
        // Arrange
        var product = new ExternalReference(Guid.NewGuid(), "Premium Lager");
        var sale = new Sale(
            "S-INT-0009",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new ExternalReference(Guid.NewGuid(), "Cust"),
            new ExternalReference(Guid.NewGuid(), "Branch"));
        sale.AddItem(product, 5, 10m);
        var originalItemId = sale.Items.Single().Id;
        await using (var ctx = CreateContext())
            await new SaleRepository(ctx).CreateAsync(sale);

        // Act: same product id, new quantity
        await using (var ctx = CreateContext())
        {
            var repo = new SaleRepository(ctx);
            var toUpdate = await repo.GetByIdAsync(sale.Id);
            toUpdate!.SyncItems(new[] { (product, 10, 10m) });
            await repo.UpdateAsync(toUpdate);
        }

        // Assert
        await using var verifyCtx = CreateContext();
        var loaded = await new SaleRepository(verifyCtx).GetByIdAsync(sale.Id);
        loaded!.Items.Should().HaveCount(1);
        loaded.Items.Single().Id.Should().Be(originalItemId);   // identity preserved across update
        loaded.Items.Single().Quantity.Should().Be(10);
    }
}
