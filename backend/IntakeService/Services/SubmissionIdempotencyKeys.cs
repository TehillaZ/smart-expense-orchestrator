using System.Globalization;

namespace IntakeService.Services;

public static class SubmissionIdempotencyKeys
{
    public static string Build(string vendor, string invoiceNumber, decimal amount)
    {
        var normalizedVendor = vendor.Trim().ToUpperInvariant();
        var normalizedInvoice = invoiceNumber.Trim().ToUpperInvariant();
        var normalizedAmount = amount.ToString("0.00", CultureInfo.InvariantCulture);

        return $"{normalizedVendor}|{normalizedInvoice}|{normalizedAmount}";
    }
}
