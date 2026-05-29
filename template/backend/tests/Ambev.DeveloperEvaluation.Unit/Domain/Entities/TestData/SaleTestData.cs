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
        var sale = new Sale
        {
            SaleNumber = $"S-{Faker.Random.Replace("########")}",
            SaleDate = Faker.Date.RecentOffset().UtcDateTime,
            Customer = new ExternalReference(Guid.NewGuid(), Faker.Person.FullName),
            Branch = new ExternalReference(Guid.NewGuid(), Faker.Company.CompanyName())
        };

        for (var i = 0; i < itemCount; i++)
            sale.AddItem(
                new ExternalReference(Guid.NewGuid(), Faker.Commerce.ProductName()),
                Faker.Random.Int(1, 20),
                decimal.Round(Faker.Random.Decimal(1, 100), 2));

        return sale;
    }
}
