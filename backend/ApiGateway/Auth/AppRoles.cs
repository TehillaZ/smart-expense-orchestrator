namespace ApiGateway.Auth;

public static class AppRoles
{
    public const string Submitter = "Submitter";
    public const string Approver = "Approver";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Submitter, Approver, Admin];

    public static bool IsValid(string? role) =>
        !string.IsNullOrWhiteSpace(role) && All.Contains(role, StringComparer.Ordinal);
}
