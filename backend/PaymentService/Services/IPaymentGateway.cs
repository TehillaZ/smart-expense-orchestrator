using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentGateway
{
    Task<PaymentReservationResult> ReserveAsync(
        string trackingId,
        string invoiceNumber,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<PaymentExecutionResult> ExecuteAsync(
        string trackingId,
        string invoiceNumber,
        decimal amount,
        string reservationId,
        CancellationToken cancellationToken = default);

    Task<PaymentCompensationResult> CompensateAsync(
        string trackingId,
        string reservationId,
        CancellationToken cancellationToken = default);
}
