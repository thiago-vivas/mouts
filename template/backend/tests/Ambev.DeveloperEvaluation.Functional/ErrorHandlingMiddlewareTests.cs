using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional;

/// <summary>
/// Unit-level tests for the centralized error handler, verifying each exception
/// type maps to the right HTTP status and the documented { type, error, detail }
/// body. Runs in-process without the full host.
/// </summary>
public class ErrorHandlingMiddlewareTests
{
    private static async Task<(int Status, JsonElement Body)> InvokeAsync(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw toThrow;
        var middleware = new ErrorHandlingMiddleware(next, Substitute.For<ILogger<ErrorHandlingMiddleware>>());

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, JsonSerializer.Deserialize<JsonElement>(json));
    }

    [Fact(DisplayName = "ValidationException maps to 400 ValidationError")]
    public async Task Validation_Maps400()
    {
        var ex = new ValidationException(new[] { new ValidationFailure("Field", "is required") });
        var (status, body) = await InvokeAsync(ex);

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.GetProperty("type").GetString().Should().Be("ValidationError");
        body.GetProperty("detail").GetString().Should().Contain("is required");
    }

    [Fact(DisplayName = "KeyNotFoundException maps to 404 ResourceNotFound")]
    public async Task NotFound_Maps404()
    {
        var (status, body) = await InvokeAsync(new KeyNotFoundException("Sale x not found"));

        status.Should().Be(StatusCodes.Status404NotFound);
        body.GetProperty("type").GetString().Should().Be("ResourceNotFound");
    }

    [Fact(DisplayName = "DomainException maps to 400 DomainRuleViolation")]
    public async Task Domain_Maps400()
    {
        var (status, body) = await InvokeAsync(new DomainException("bad rule"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.GetProperty("type").GetString().Should().Be("DomainRuleViolation");
    }

    [Fact(DisplayName = "InvalidOperationException maps to 400 InvalidOperation")]
    public async Task InvalidOperation_Maps400()
    {
        var (status, body) = await InvokeAsync(new InvalidOperationException("nope"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.GetProperty("type").GetString().Should().Be("InvalidOperation");
    }

    [Fact(DisplayName = "UnauthorizedAccessException maps to 401 AuthenticationError")]
    public async Task Unauthorized_Maps401()
    {
        var (status, body) = await InvokeAsync(new UnauthorizedAccessException("no"));

        status.Should().Be(StatusCodes.Status401Unauthorized);
        body.GetProperty("type").GetString().Should().Be("AuthenticationError");
    }

    [Fact(DisplayName = "Unexpected exception maps to 500 ServerError")]
    public async Task Unexpected_Maps500()
    {
        var (status, body) = await InvokeAsync(new Exception("boom"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.GetProperty("type").GetString().Should().Be("ServerError");
    }

    [Fact(DisplayName = "No exception passes through untouched")]
    public async Task NoException_PassesThrough()
    {
        var context = new DefaultHttpContext();
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };
        var middleware = new ErrorHandlingMiddleware(next, Substitute.For<ILogger<ErrorHandlingMiddleware>>());

        await middleware.InvokeAsync(context);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
