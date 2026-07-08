using System.Diagnostics;

namespace ApprovalFlow.Observability;

public static class ObservabilityActivitySources
{
    public const string Name = "ApprovalFlow";

    public static readonly ActivitySource Instance = new(Name, "1.0.0");
}
