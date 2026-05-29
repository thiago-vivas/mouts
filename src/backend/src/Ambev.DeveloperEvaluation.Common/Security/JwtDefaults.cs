namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Default JWT issuer/audience used when not overridden via configuration
/// (Jwt:Issuer / Jwt:Audience). Shared by the token generator and the bearer
/// validation so the two can never drift apart.
/// </summary>
public static class JwtDefaults
{
    public const string Issuer = "DeveloperStore";
    public const string Audience = "DeveloperStoreClient";
}
