using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Seeding;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Seeding;

public class DbSeederTests
{
    private readonly string _databaseName = $"seed-{Guid.NewGuid()}";

    private DefaultContext CreateContext()
        => new(new DbContextOptionsBuilder<DefaultContext>().UseInMemoryDatabase(_databaseName).Options);

    [Fact(DisplayName = "Seeder inserts the sample sales on an empty database")]
    public async Task Given_EmptyDb_When_Seeded_Then_SalesInserted()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        await DbSeeder.SeedAsync(ctx);

        // Assert
        (await ctx.Sales.CountAsync()).Should().Be(4);
        (await ctx.Sales.CountAsync(s => s.IsCancelled)).Should().Be(1);
    }

    [Fact(DisplayName = "Seeder is idempotent — running twice does not duplicate")]
    public async Task Given_SeededDb_When_SeededAgain_Then_NoDuplicates()
    {
        // Arrange
        await using (var ctx = CreateContext())
            await DbSeeder.SeedAsync(ctx);

        // Act
        await using (var ctx = CreateContext())
            await DbSeeder.SeedAsync(ctx);

        // Assert
        await using var verify = CreateContext();
        (await verify.Sales.CountAsync()).Should().Be(4);
    }
}
