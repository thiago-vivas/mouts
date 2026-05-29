namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Generates unique, human-readable sale numbers in the form
/// <c>S-yyyyMMddHHmmssfff-XXXXXXXX</c> where the suffix is a random hex token.
/// Timestamp ordering keeps numbers roughly sequential while the 32-bit suffix
/// avoids collisions for sales created within the same millisecond.
/// </summary>
public static class SaleNumberGenerator
{
    public static string Next()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"S-{timestamp}-{suffix}";
    }
}
