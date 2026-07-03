using PaymentService.Models;

namespace PaymentService.Services;

public sealed class PaymentSagaProcessor
{
    private readonly IPaymentSagaStore sagaStore;
    private readonly ISubmissionWorkflowStore workflowStore;
    private readonly IPaymentGateway paymentGateway;
    private readonly IPaymentOutcomePublisher outcomePublisher;
    private readonly ILogger<PaymentSagaProcessor> logger;

    public PaymentSagaProcessor(
        IPaymentSagaStore sagaStore,
        ISubmissionWorkflowStore workflowStore,
        IPaymentGateway paymentGateway,
        IPaymentOutcomePublisher outcomePublisher,
        ILogger<PaymentSagaProcessor> logger)
    {
        this.sagaStore = sagaStore;
        this.workflowStore = workflowStore;
        this.paymentGateway = paymentGateway;
        this.outcomePublisher = outcomePublisher;
        this.logger = logger;
    }

    public async Task ProcessAsync(PaymentRequestedEvent paymentEvent)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = paymentEvent.CorrelationId,
            ["TrackingId"] = paymentEvent.TrackingId
        });

        var idempotencyKey = string.IsNullOrWhiteSpace(paymentEvent.IdempotencyKey)
            ? paymentEvent.TrackingId
            : paymentEvent.IdempotencyKey.Trim();

        var existingSaga = await sagaStore.GetAsync(idempotencyKey);
        if (existingSaga is not null)
        {
            if (existingSaga.IsTerminal)
            {
                logger.LogInformation(
                    "Payment idempotency key {IdempotencyKey} already reached terminal status {Status}. Skipping duplicate processing.",
                    idempotencyKey,
                    existingSaga.Status);
                return;
            }

            logger.LogInformation(
                "Payment idempotency key {IdempotencyKey} is already in progress at phase {Phase}. Skipping duplicate processing.",
                idempotencyKey,
                existingSaga.Phase);
            return;
        }

        var submission = await workflowStore.GetAsync(paymentEvent.TrackingId);
        if (submission is null)
        {
            logger.LogWarning(
                "Submission {TrackingId} was not found when payment was requested.",
                paymentEvent.TrackingId);
            return;
        }

        if (!IsApprovedForPayment(submission.Status))
        {
            logger.LogWarning(
                "Submission {TrackingId} has status {Status} and is not eligible for payment.",
                paymentEvent.TrackingId,
                submission.Status);
            return;
        }

        var pendingSubmission = submission with
        {
            Status = PaymentStatuses.PaymentPending,
            Reason = "Approved expense is queued for payment processing."
        };
        await workflowStore.SaveAsync(pendingSubmission);

        var saga = existingSaga ?? new PaymentSagaState(
            paymentEvent.TrackingId,
            idempotencyKey,
            paymentEvent.CorrelationId,
            paymentEvent.Amount,
            paymentEvent.InvoiceNumber,
            PaymentSagaPhases.Started,
            PaymentStatuses.PaymentPending,
            pendingSubmission.Reason,
            null,
            DateTimeOffset.UtcNow);

        await sagaStore.SaveAsync(saga);

        var reservation = await paymentGateway.ReserveAsync(
            paymentEvent.TrackingId,
            paymentEvent.InvoiceNumber,
            paymentEvent.Amount);

        if (!reservation.Success)
        {
        await MarkPaymentFailedAsync(
            pendingSubmission,
            saga,
            reservation.Reason,
            PaymentSagaPhases.Failed,
            PaymentStatuses.PaymentFailed);
            return;
        }

        saga = saga with
        {
            Phase = PaymentSagaPhases.Reserved,
            ReservationId = reservation.ReservationId,
            Reason = reservation.Reason,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await sagaStore.SaveAsync(saga);

        var execution = await paymentGateway.ExecuteAsync(
            paymentEvent.TrackingId,
            paymentEvent.InvoiceNumber,
            paymentEvent.Amount,
            reservation.ReservationId!);

        if (execution.Success)
        {
            var paidSubmission = pendingSubmission with
            {
                Status = PaymentStatuses.Paid,
                Reason = execution.Reason
            };
            await workflowStore.SaveAsync(paidSubmission);

            saga = saga with
            {
                Phase = PaymentSagaPhases.Completed,
                Status = PaymentStatuses.Paid,
                Reason = execution.Reason,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await sagaStore.SaveAsync(saga);

            await outcomePublisher.PublishCompletedAsync(
                new PaymentCompletedEvent(
                    paymentEvent.TrackingId,
                    paymentEvent.CorrelationId,
                    paidSubmission.Status,
                    paidSubmission.Reason,
                    paymentEvent.Amount));

            logger.LogInformation(
                "Payment saga completed successfully for submission {TrackingId}.",
                paymentEvent.TrackingId);
            return;
        }

        var compensation = await paymentGateway.CompensateAsync(
            paymentEvent.TrackingId,
            reservation.ReservationId!);

        var finalStatus = compensation.Success
            ? PaymentStatuses.Compensated
            : PaymentStatuses.PaymentFailed;

        var finalReason = compensation.Success
            ? $"{execution.Reason} Reservation was compensated: {compensation.Reason}"
            : $"{execution.Reason} Compensation failed: {compensation.Reason}";

        await MarkPaymentFailedAsync(
            pendingSubmission,
            saga with
            {
                Phase = compensation.Success ? PaymentSagaPhases.Compensated : PaymentSagaPhases.Failed,
                Status = finalStatus,
                Reason = finalReason,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            finalReason,
            compensation.Success ? PaymentSagaPhases.Compensated : PaymentSagaPhases.Failed,
            finalStatus);
    }

    private async Task MarkPaymentFailedAsync(
        SubmissionWorkflowState pendingSubmission,
        PaymentSagaState saga,
        string reason,
        string phase,
        string finalStatus)
    {
        var status = finalStatus;
        var failedSubmission = pendingSubmission with
        {
            Status = status,
            Reason = reason
        };
        await workflowStore.SaveAsync(failedSubmission);

        var updatedSaga = saga with
        {
            Phase = phase,
            Status = status,
            Reason = reason,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await sagaStore.SaveAsync(updatedSaga);

        await outcomePublisher.PublishFailedAsync(
            new PaymentFailedEvent(
                pendingSubmission.TrackingId,
                pendingSubmission.CorrelationId,
                status,
                reason,
                pendingSubmission.Amount));

        logger.LogWarning(
            "Payment saga failed for submission {TrackingId} with status {Status}.",
            pendingSubmission.TrackingId,
            status);
    }

    private static bool IsApprovedForPayment(string status)
    {
        return string.Equals(status, ApprovalOutcomeStatuses.AutoApproved, StringComparison.Ordinal)
            || string.Equals(status, ApprovalOutcomeStatuses.HumanApproved, StringComparison.Ordinal);
    }
}
