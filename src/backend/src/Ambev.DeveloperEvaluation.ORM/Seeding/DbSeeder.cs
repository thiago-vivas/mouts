using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Seeding;

/// <summary>
/// Seeds a small, deterministic set of sales so the API has data to query out of
/// the box. Idempotent: it only seeds when the Sales table is empty. Sales are
/// built through the domain aggregate so discounts/totals are computed by the
/// real business rules.
/// </summary>
public static class DbSeeder
{
    // Fixed external identities (customers / branches / products) so the seed is
    // reproducible. These are factory methods that return a FRESH instance on every
    // call: EF Core owns each ExternalReference under its sale/item, so the same
    // instance must never be shared across owners (doing so leaves the owned FK null).
    private static ExternalReference CustomerJohn() => new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "John Doe");
    private static ExternalReference CustomerMary() => new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mary Jane");
    private static ExternalReference CustomerAcme() => new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Acme Corporation");

    private static ExternalReference BranchDowntown() => new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Downtown Store");
    private static ExternalReference BranchAirport() => new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Airport Store");

    private static ExternalReference ProductBeer() => new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Premium Lager 600ml");
    private static ExternalReference ProductSoda() => new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Sparkling Soda 350ml");
    private static ExternalReference ProductWater() => new(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "Mineral Water 500ml");

    public static async Task SeedAsync(DefaultContext context, CancellationToken cancellationToken = default)
    {
        await SeedDefaultUserAsync(context, cancellationToken);

        if (await context.Sales.AnyAsync(cancellationToken))
            return;

        var sales = new List<Sale>();

        // Sale 1 — small quantities, no discount tier triggered for water (qty 2),
        // 10% tier for beer (qty 5).
        var sale1 = NewSale("S-2026-0001", new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            CustomerJohn(), BranchDowntown(),
            Guid.Parse("f0000001-0000-0000-0000-000000000001"));
        sale1.AddItem(ProductBeer(), 5, 8.50m);   // 10% discount
        sale1.AddItem(ProductWater(), 2, 2.00m);  // no discount
        sales.Add(sale1);

        // Sale 2 — 20% tier (qty 12) and the above-4 boundary (qty 4 = no discount).
        var sale2 = NewSale("S-2026-0002", new DateTime(2026, 2, 3, 14, 0, 0, DateTimeKind.Utc),
            CustomerMary(), BranchAirport(),
            Guid.Parse("f0000002-0000-0000-0000-000000000002"));
        sale2.AddItem(ProductSoda(), 12, 3.25m);  // 20% discount
        sale2.AddItem(ProductBeer(), 4, 8.50m);   // no discount (qty 4 is not "above 4")
        sales.Add(sale2);

        // Sale 3 — bulk order at the 20% tier (qty 20 max).
        var sale3 = NewSale("S-2026-0003", new DateTime(2026, 3, 21, 9, 15, 0, DateTimeKind.Utc),
            CustomerAcme(), BranchDowntown(),
            Guid.Parse("f0000003-0000-0000-0000-000000000003"));
        sale3.AddItem(ProductWater(), 20, 2.00m); // 20% discount
        sales.Add(sale3);

        // Sale 4 — already cancelled, to exercise the cancelled flag in listings.
        var sale4 = NewSale("S-2026-0004", new DateTime(2026, 4, 5, 18, 45, 0, DateTimeKind.Utc),
            CustomerJohn(), BranchAirport(),
            Guid.Parse("f0000004-0000-0000-0000-000000000004"));
        sale4.AddItem(ProductSoda(), 3, 3.25m);   // no discount
        sale4.Cancel();
        sales.Add(sale4);

        await context.Sales.AddRangeAsync(sales, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds a default admin user so the API (and the Angular frontend) can log in
    /// out of the box: admin@developerstore.com / Admin@123. Idempotent.
    /// </summary>
    private static async Task SeedDefaultUserAsync(DefaultContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken))
            return;

        var hasher = new BCryptPasswordHasher();
        context.Users.Add(new User
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Username = "admin",
            Email = "admin@developerstore.com",
            Phone = "+5511999999999",
            Password = hasher.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            Status = UserStatus.Active
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static Sale NewSale(string number, DateTime date, ExternalReference customer,
        ExternalReference branch, Guid id)
    {
        return new Sale(number, date, customer, branch)
        {
            Id = id,
            CreatedAt = date
        };
    }
}
