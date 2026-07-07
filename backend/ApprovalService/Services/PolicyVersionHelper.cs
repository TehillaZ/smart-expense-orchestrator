namespace ApprovalService.Services;

public static class PolicyVersionHelper
{
    public static string Bump(string currentVersion)
    {
        if (currentVersion.StartsWith('v')
            && int.TryParse(currentVersion[1..], out var versionNumber))
        {
            return $"v{versionNumber + 1}";
        }

        return "v2";
    }
}
