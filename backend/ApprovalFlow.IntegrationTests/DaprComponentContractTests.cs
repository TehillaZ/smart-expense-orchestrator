using ApprovalFlow.TestSupport;

namespace ApprovalFlow.IntegrationTests;

[Trait(TestLayers.TraitName, TestLayers.Integration)]
public class DaprComponentContractTests
{
    private static readonly string ComponentsDirectory = ResolveComponentsDirectory();

    [Fact]
    public void ApprovalStateComponent_UsesSharedRedisStoreNameAndScopes()
    {
        var yaml = File.ReadAllText(Path.Combine(ComponentsDirectory, "approval-state.yaml"));

        Assert.Contains("name: approval-state", yaml, StringComparison.Ordinal);
        Assert.Contains("type: state.redis", yaml, StringComparison.Ordinal);
        Assert.Contains("keyPrefix", yaml, StringComparison.Ordinal);
        Assert.Contains("submission", yaml, StringComparison.Ordinal);
        Assert.Contains("intakeservice", yaml, StringComparison.Ordinal);
        Assert.Contains("approvalservice", yaml, StringComparison.Ordinal);
        Assert.Contains("paymentservice", yaml, StringComparison.Ordinal);
        Assert.Contains("notificationservice", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderPubSubComponent_UsesSharedRedisPubSubNameAndScopes()
    {
        var yaml = File.ReadAllText(Path.Combine(ComponentsDirectory, "order-pubsub.yaml"));

        Assert.Contains("name: order-pubsub", yaml, StringComparison.Ordinal);
        Assert.Contains("type: pubsub.redis", yaml, StringComparison.Ordinal);
        Assert.Contains("intakeservice", yaml, StringComparison.Ordinal);
        Assert.Contains("approvalservice", yaml, StringComparison.Ordinal);
        Assert.Contains("paymentservice", yaml, StringComparison.Ordinal);
        Assert.Contains("notificationservice", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalSecretStoreComponent_IsPresentForServiceSecrets()
    {
        var path = Path.Combine(ComponentsDirectory, "local-secret-store.yaml");
        Assert.True(File.Exists(path), $"Expected Dapr secret store component at {path}");

        var yaml = File.ReadAllText(path);
        Assert.Contains("name:", yaml, StringComparison.Ordinal);
        Assert.Contains("secretstores", yaml, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveComponentsDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "dapr"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "infra", "dapr", "components")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "infra", "dapr", "components")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "infra", "dapr", "components")),
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "approval-state.yaml")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate infra/dapr/components for integration contract tests.");
    }
}
