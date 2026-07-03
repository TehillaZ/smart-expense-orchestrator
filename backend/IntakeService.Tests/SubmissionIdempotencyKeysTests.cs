using IntakeService.Services;

namespace IntakeService.Tests;

public class SubmissionIdempotencyKeysTests
{
    [Fact]
    public void Build_WithDifferentCasing_ProducesSameKey()
    {
        var first = SubmissionIdempotencyKeys.Build("Acme Corp", "inv-1007", 120m);
        var second = SubmissionIdempotencyKeys.Build("ACME CORP", "INV-1007", 120.00m);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Build_WithDifferentAmount_ProducesDifferentKey()
    {
        var first = SubmissionIdempotencyKeys.Build("Acme Corp", "INV-1007", 120m);
        var second = SubmissionIdempotencyKeys.Build("Acme Corp", "INV-1007", 121m);

        Assert.NotEqual(first, second);
    }
}
