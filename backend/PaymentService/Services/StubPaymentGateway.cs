using Microsoft.Extensions.Options;
using PaymentService.Models;

namespace PaymentService.Services;

public sealed class PaymentSimulationOptions
{
    public const string SectionName = "PaymentSimulation";

    public string[] ForcedFailureInvoiceNumbers { get; set; } = ["RS-90021"];
}

public sealed class StubPaymentGateway : IPaymentGateway
{
    private readonly PaymentSimulationOptions options;
    private readonly ILogger<StubPaymentGateway> logger;

    public StubPaymentGateway(IOptions<PaymentSimulationOptions> options, ILogger<StubPaymentGateway> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public Task<PaymentReservationResult> ReserveAsync(
        string trackingId,
        string invoiceNumber,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return Task.FromResult(new PaymentReservationResult(
                false,
                null,
                "Payment amount must be greater than zero."));
        }

        var reservationId = $"res-{trackingId}";
        logger.LogInformation(
            "Reserved {Amount:C} for submission {TrackingId} with reservation {ReservationId}.",
            amount,
            trackingId,
            reservationId);

        return Task.FromResult(new PaymentReservationResult(
            true,
            reservationId,
            $"Reserved {amount:C} for invoice {invoiceNumber}."));
    }

    public Task<PaymentExecutionResult> ExecuteAsync(
        string trackingId,
        string invoiceNumber,
        decimal amount,
        string reservationId,
        CancellationToken cancellationToken = default)
    {
        if (ShouldForceFailure(invoiceNumber))
        {
            logger.LogWarning(
                "Simulated payment failure for submission {TrackingId} and invoice {InvoiceNumber}.",
                trackingId,
                invoiceNumber);

            return Task.FromResult(new PaymentExecutionResult(
                false,
                $"Simulated payment processor failure for invoice {invoiceNumber}."));
        }

        logger.LogInformation(
            "Executed payment of {Amount:C} for submission {TrackingId} using reservation {ReservationId}.",
            amount,
            trackingId,
            reservationId);

        return Task.FromResult(new PaymentExecutionResult(
            true,
            $"Payment of {amount:C} completed successfully."));
    }

    public Task<PaymentCompensationResult> CompensateAsync(
        string trackingId,
        string reservationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Compensated reservation {ReservationId} for submission {TrackingId}.",
            reservationId,
            trackingId);

        return Task.FromResult(new PaymentCompensationResult(
            true,
            $"Reservation {reservationId} was released."));
    }

    private bool ShouldForceFailure(string invoiceNumber)
    {
        return options.ForcedFailureInvoiceNumbers.Any(forcedInvoice =>
            string.Equals(forcedInvoice, invoiceNumber, StringComparison.OrdinalIgnoreCase));
    }
}
