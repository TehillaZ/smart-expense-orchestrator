using IntakeService.Models;

namespace IntakeService.Services;

public static class SubmissionValidator
{
    public static IReadOnlyList<string> Validate(SubmissionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Vendor))
        {
            errors.Add("vendor is required.");
        }

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
        {
            errors.Add("invoiceNumber is required.");
        }

        if (request.Amount <= 0)
        {
            errors.Add("amount must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            errors.Add("category is required.");
        }

        return errors;
    }
}
