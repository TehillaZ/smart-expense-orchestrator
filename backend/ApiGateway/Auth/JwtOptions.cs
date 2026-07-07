namespace ApiGateway.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ApprovalFlow";
    public string Audience { get; init; } = "ApprovalFlow";
    public string SigningKey { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 480;
}
