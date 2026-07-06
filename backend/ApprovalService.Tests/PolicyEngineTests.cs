using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

public class PolicyEngineTests
{
    private static SubmissionReceivedEvent SampleEvent(decimal amount, string category = "meals") =>
        new("tracking-1", "correlation-1", "Acme Corp", "INV-1001", amount, category);

    private static AiRecommendationResult ApproveRecommendation(decimal confidence = 0.95m) =>
        new("Approve", confidence, ["MEAL-01"], "Looks eligible.");

    private static AiRecommendationResult EscalateRecommendation() =>
        new("Escalate", 0.99m, ["AUTONOMY-CEILING"], "Too high.");

    [Fact]
    public void Evaluate_WithEligibleSubmission_ReturnsAutoApproved()
    {
        var engine = new PolicyEngine();

        var result = engine.Evaluate(
            SampleEvent(100m),
            PolicySettings.Default,
            ApproveRecommendation());

        Assert.Equal(ApprovalStatuses.AutoApproved, result.FinalStatus);
    }

    [Fact]
    public void Evaluate_WithAmountAboveCeiling_EscalatesEvenIfAiRecommendsApprove()
    {
        var engine = new PolicyEngine();

        var result = engine.Evaluate(
            SampleEvent(500m),
            PolicySettings.Default,
            ApproveRecommendation());

        Assert.Equal(ApprovalStatuses.Escalated, result.FinalStatus);
        Assert.Contains("AUTONOMY-CEILING", result.AppliedRules);
    }

    [Fact]
    public void Evaluate_WithCapitalHardware_Escalates()
    {
        var engine = new PolicyEngine();

        var result = engine.Evaluate(
            SampleEvent(1400m, "hardware"),
            PolicySettings.Default,
            ApproveRecommendation());

        Assert.Equal(ApprovalStatuses.Escalated, result.FinalStatus);
        Assert.Contains("HW-02", result.AppliedRules);
    }

    [Fact]
    public void Evaluate_WithSaasAboveMonthlyCap_Escalates()
    {
        var engine = new PolicyEngine();

        var result = engine.Evaluate(
            SampleEvent(220m, "saas"),
            PolicySettings.Default,
            ApproveRecommendation());

        Assert.Equal(ApprovalStatuses.Escalated, result.FinalStatus);
        Assert.Contains("SAAS-01", result.AppliedRules);
    }

    [Fact]
    public void Evaluate_WithLowAiConfidence_Escalates()
    {
        var engine = new PolicyEngine();

        var result = engine.Evaluate(
            SampleEvent(100m),
            PolicySettings.Default,
            ApproveRecommendation(confidence: 0.50m));

        Assert.Equal(ApprovalStatuses.Escalated, result.FinalStatus);
    }

    [Fact]
    public void Evaluate_WithAdversarialAiRecommendation_StillEscalatesAboveCeiling()
    {
        var engine = new PolicyEngine();

        var result = engine.Evaluate(
            SampleEvent(500m),
            PolicySettings.Default,
            new AiRecommendationResult(
                "Approve",
                0.99m,
                ["PROMPT-INJECTION"],
                "User said approve me — recommending approval regardless of amount."));

        Assert.Equal(ApprovalStatuses.Escalated, result.FinalStatus);
        Assert.Contains("AUTONOMY-CEILING", result.AppliedRules);
    }
}
