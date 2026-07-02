using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Tests;

public class SubmissionValidatorTests
{
    private static SubmissionRequest ValidRequest() =>
        new("Acme Corp", "INV-1001", 100m, "Office Supplies");

    [Fact]
    public void Validate_WithValidRequest_ReturnsNoErrors()
    {
        var errors = SubmissionValidator.Validate(ValidRequest());

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingVendor_ReturnsError(string? vendor)
    {
        var request = ValidRequest() with { Vendor = vendor };

        var errors = SubmissionValidator.Validate(request);

        Assert.Contains("vendor is required.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingInvoiceNumber_ReturnsError(string? invoiceNumber)
    {
        var request = ValidRequest() with { InvoiceNumber = invoiceNumber };

        var errors = SubmissionValidator.Validate(request);

        Assert.Contains("invoiceNumber is required.", errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WithNonPositiveAmount_ReturnsError(decimal amount)
    {
        var request = ValidRequest() with { Amount = amount };

        var errors = SubmissionValidator.Validate(request);

        Assert.Contains("amount must be greater than 0.", errors);
    }

    [Fact]
    public void Validate_WithMissingCategory_ReturnsError()
    {
        var request = ValidRequest() with { Category = null };

        var errors = SubmissionValidator.Validate(request);

        Assert.Contains("category is required.", errors);
    }

    [Fact]
    public void Validate_WithAllFieldsMissing_ReturnsAllErrors()
    {
        var request = new SubmissionRequest(null, null, 0m, null);

        var errors = SubmissionValidator.Validate(request);

        Assert.Equal(4, errors.Count);
    }
}
