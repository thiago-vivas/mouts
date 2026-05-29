using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

/// <summary>
/// End-to-end HTTP tests for the Sales API covering the full lifecycle
/// (create → get → list → update → cancel item → cancel sale) plus error paths.
/// </summary>
public class SalesApiTests : IClassFixture<SalesApiFixture>
{
    private readonly HttpClient _client;
    private readonly HttpClient _anonymous;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public SalesApiTests(SalesApiFixture fixture)
    {
        _client = fixture.Client;
        _anonymous = fixture.AnonymousClient;
    }

    [Fact(DisplayName = "Requests without a JWT are rejected with 401")]
    public async Task NoToken_Returns401()
    {
        // Act
        var response = await _anonymous.GetAsync("/api/sales");
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static object NewSaleBody(int quantity = 12, decimal unitPrice = 8.50m) => new
    {
        saleDate = "2026-05-28T10:00:00Z",
        customer = new { id = Guid.NewGuid(), name = "John Doe" },
        branch = new { id = Guid.NewGuid(), name = "Downtown Store" },
        items = new[]
        {
            new { productId = Guid.NewGuid(), productName = "Premium Lager", quantity, unitPrice }
        }
    };

    private async Task<JsonElement> CreateSaleAsync(int quantity = 12, decimal unitPrice = 8.50m)
    {
        var response = await _client.PostAsJsonAsync("/api/sales", NewSaleBody(quantity, unitPrice));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return doc.GetProperty("data");
    }

    [Fact(DisplayName = "POST creates a sale with computed discount and returns 201")]
    public async Task Post_CreatesSale()
    {
        // Act
        var data = await CreateSaleAsync(quantity: 12, unitPrice: 10m);

        // Assert
        data.GetProperty("saleNumber").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("items")[0].GetProperty("discount").GetDecimal().Should().Be(24m); // 20% of 120
        data.GetProperty("totalAmount").GetDecimal().Should().Be(96m);
    }

    [Fact(DisplayName = "GET by id returns the created sale")]
    public async Task Get_ReturnsSale()
    {
        // Arrange
        var created = await CreateSaleAsync();
        var id = created.GetProperty("id").GetGuid();

        // Act
        var response = await _client.GetAsync($"/api/sales/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("data");
        data.GetProperty("id").GetGuid().Should().Be(id);
    }

    [Fact(DisplayName = "GET list returns a paginated envelope")]
    public async Task List_ReturnsPaginated()
    {
        // Arrange
        await CreateSaleAsync();

        // Act
        var response = await _client.GetAsync("/api/sales?_page=1&_size=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        doc.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
        doc.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
        doc.GetProperty("totalItems").GetInt32().Should().BeGreaterThan(0); // per general-api.md
        doc.GetProperty("currentPage").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "PUT updates a sale and recomputes the total")]
    public async Task Put_UpdatesSale()
    {
        // Arrange
        var created = await CreateSaleAsync();
        var id = created.GetProperty("id").GetGuid();

        var body = new
        {
            saleDate = "2026-05-29T10:00:00Z",
            customer = new { id = Guid.NewGuid(), name = "Jane Roe" },
            branch = new { id = Guid.NewGuid(), name = "Airport Store" },
            items = new[]
            {
                new { productId = Guid.NewGuid(), productName = "Water", quantity = 3, unitPrice = 2m }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/sales/{id}", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("data");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(6m); // 3 * 2, no discount
        data.GetProperty("customer").GetProperty("name").GetString().Should().Be("Jane Roe");
    }

    [Fact(DisplayName = "PATCH cancels the whole sale")]
    public async Task Patch_CancelsSale()
    {
        // Arrange
        var created = await CreateSaleAsync();
        var id = created.GetProperty("id").GetGuid();

        // Act
        var response = await _client.PatchAsync($"/api/sales/{id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("data");
        data.GetProperty("isCancelled").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "PATCH cancels a single item and recomputes total")]
    public async Task Patch_CancelsItem()
    {
        // Arrange
        var created = await CreateSaleAsync(quantity: 5, unitPrice: 10m); // total 45
        var id = created.GetProperty("id").GetGuid();
        var itemId = created.GetProperty("items")[0].GetProperty("id").GetGuid();

        // Act
        var response = await _client.PatchAsync($"/api/sales/{id}/items/{itemId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("data");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(0m);
        data.GetProperty("items")[0].GetProperty("isCancelled").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "DELETE removes a sale")]
    public async Task Delete_RemovesSale()
    {
        // Arrange
        var created = await CreateSaleAsync();
        var id = created.GetProperty("id").GetGuid();

        // Act
        var delete = await _client.DeleteAsync($"/api/sales/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert
        var get = await _client.GetAsync($"/api/sales/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "POST with quantity above 20 returns 400 with error envelope")]
    public async Task Post_QuantityAbove20_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/sales", NewSaleBody(quantity: 21));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        doc.GetProperty("type").GetString().Should().NotBeNullOrEmpty();
        doc.GetProperty("detail").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "GET unknown id returns 404 ResourceNotFound")]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        doc.GetProperty("type").GetString().Should().Be("ResourceNotFound");
    }

    [Fact(DisplayName = "PUT on unknown id returns 404")]
    public async Task Put_UnknownId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PutAsJsonAsync($"/api/sales/{Guid.NewGuid()}", NewSaleBody(quantity: 5));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Cancelling an already-cancelled item returns 400 DomainRuleViolation")]
    public async Task CancelItem_Twice_ReturnsDomainRuleViolation()
    {
        // Arrange
        var created = await CreateSaleAsync(quantity: 5, unitPrice: 10m);
        var id = created.GetProperty("id").GetGuid();
        var itemId = created.GetProperty("items")[0].GetProperty("id").GetGuid();

        (await _client.PatchAsync($"/api/sales/{id}/items/{itemId}/cancel", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var second = await _client.PatchAsync($"/api/sales/{id}/items/{itemId}/cancel", null);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var doc = await second.Content.ReadFromJsonAsync<JsonElement>(Json);
        doc.GetProperty("type").GetString().Should().Be("DomainRuleViolation");
    }

    [Fact(DisplayName = "List filters by customer name and returns only the matching sale")]
    public async Task List_FilterByCustomerName()
    {
        // Arrange: a globally-unique customer name so the exact-match filter is deterministic
        // even though the in-memory database is shared across the test class.
        var uniqueCustomer = $"Filter Subject {Guid.NewGuid():N}";
        var body = new
        {
            saleDate = "2026-05-28T10:00:00Z",
            customer = new { id = Guid.NewGuid(), name = uniqueCustomer },
            branch = new { id = Guid.NewGuid(), name = "Downtown Store" },
            items = new[] { new { productId = Guid.NewGuid(), productName = "Premium Lager", quantity = 5, unitPrice = 10m } }
        };
        (await _client.PostAsJsonAsync("/api/sales", body)).StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync($"/api/sales?customerName={Uri.EscapeDataString(uniqueCustomer)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("data");
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("customer").GetProperty("name").GetString().Should().Be(uniqueCustomer);
    }

    [Fact(DisplayName = "DELETE on an unknown id returns 404")]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/sales/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Cancelling an already-cancelled sale returns 400 DomainRuleViolation")]
    public async Task Cancel_AlreadyCancelledSale_ReturnsDomainRuleViolation()
    {
        // Arrange
        var created = await CreateSaleAsync();
        var id = created.GetProperty("id").GetGuid();
        (await _client.PatchAsync($"/api/sales/{id}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var second = await _client.PatchAsync($"/api/sales/{id}/cancel", null);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var doc = await second.Content.ReadFromJsonAsync<JsonElement>(Json);
        doc.GetProperty("type").GetString().Should().Be("DomainRuleViolation");
    }

    [Fact(DisplayName = "POST /api/auth with the seeded admin returns a JWT")]
    public async Task Authenticate_WithSeededAdmin_ReturnsToken()
    {
        // Act (login needs no bearer token, so the anonymous client is used)
        var response = await _anonymous.PostAsJsonAsync("/api/auth",
            new { email = "admin@developerstore.com", password = "Admin@123" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("data");
        data.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("email").GetString().Should().Be("admin@developerstore.com");
    }

    [Fact(DisplayName = "POST /api/auth with bad credentials does not return 500")]
    public async Task Authenticate_WithBadCredentials_DoesNotServerError()
    {
        // Act
        var response = await _anonymous.PostAsJsonAsync("/api/auth",
            new { email = "admin@developerstore.com", password = "wrong-password" });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }
}
