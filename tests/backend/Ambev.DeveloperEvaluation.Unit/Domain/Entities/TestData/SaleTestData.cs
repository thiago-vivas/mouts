using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>Bogus-backed builder for <see cref="Sale"/> aggregates used in tests.</summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    public static Sale GenerateValidSale(int itemCount = 2)
    {
        var sale = new Sale(
            $"S-{Faker.Random.Replace("########")}",
            Faker.Date.RecentOffset().UtcDateTime,
            new ExternalReference(Guid.NewGuid(), Faker.Person.FullName),
            new ExternalReference(Guid.NewGuid(), Faker.Company.CompanyName()));

        for (var i = 0; i < itemCount; i++)
            sale.AddItem(
                new ExternalReference(Guid.NewGuid(), Faker.Commerce.ProductName()),
                Faker.Random.Int(1, 20),
                decimal.Round(Faker.Random.Decimal(1, 100), 2));

        return sale;
    }
}
