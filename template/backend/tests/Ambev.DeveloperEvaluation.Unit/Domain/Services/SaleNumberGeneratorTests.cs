using Ambev.DeveloperEvaluation.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

public class SaleNumberGeneratorTests
{
    [Fact(DisplayName = "Generated sale number has the expected prefix and shape")]
    public void Given_Generated_Then_HasExpectedFormat()
    {
        var number = SaleNumberGenerator.Next();

        number.Should().StartWith("S-");
        number.Should().MatchRegex(@"^S-\d{17}-[0-9A-F]{8}$");
    }

    [Fact(DisplayName = "Generated sale numbers are unique across many calls")]
    public void Given_ManyCalls_Then_AllUnique()
    {
        var numbers = Enumerable.Range(0, 1000).Select(_ => SaleNumberGenerator.Next()).ToList();
        numbers.Distinct().Should().HaveCount(numbers.Count);
    }
}
