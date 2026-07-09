namespace ApprovalFlow.TestSupport;

/// <summary>
/// Shared xUnit trait values for multi-layer test filtering (N6).
/// Use: [Trait(TestLayers.TraitName, TestLayers.Unit)]
/// Filter: --filter "Category=Unit" or "Category!=Journey"
/// </summary>
public static class TestLayers
{
    public const string TraitName = "Category";

    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string Api = "Api";
    public const string Journey = "Journey";
}
