using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Tests;

public class PaymentSagaProcessorTests
{
    private static PaymentRequestedEvent SampleEvent(
        string trackingId = "tracking-1",
        string invoiceNumber = "INV-1001",
        decimal amount = 100m) =>
        new(trackingId, "correlation-1", "Acme Corp", invoiceNumber, amount, "meals", trackingId);

    private static SubmissionWorkflowState ApprovedState(string trackingId = "tracking-1", string status = ApprovalOutcomeStatuses.AutoApproved) =>
        new(trackingId, "correlation-1", "Acme Corp", "INV-1001", 100m, "meals", status, "Approved.");

    [Fact]
    public async Task ProcessAsync_WithApprovedSubmission_MarksPaidAndPublishesCompleted()
    {
        var sagaStore = new FakePaymentSagaStore();
        var workflowStore = new FakeSubmissionWorkflowStore();
        workflowStore.Seed(ApprovedState());
        var publisher = new FakePaymentOutcomePublisher();
        var processor = CreateProcessor(sagaStore, workflowStore, publisher);

        await processor.ProcessAsync(SampleEvent());

        Assert.Equal(PaymentStatuses.Paid, workflowStore.LastSaved!.Status);
        Assert.Equal(PaymentStatuses.Paid, sagaStore.LastSaved!.Status);
        Assert.NotNull(publisher.LastCompleted);
        Assert.Null(publisher.LastFailed);
    }

    [Fact]
    public async Task ProcessAsync_WithForcedFailureInvoice_CompensatesAndMarksCompensated()
    {
        var sagaStore = new FakePaymentSagaStore();
        var workflowStore = new FakeSubmissionWorkflowStore();
        workflowStore.Seed(ApprovedState(status: ApprovalOutcomeStatuses.HumanApproved) with
        {
            InvoiceNumber = "RS-90021",
            Amount = 9500m,
            Reason = "Human approved."
        });
        var publisher = new FakePaymentOutcomePublisher();
        var processor = CreateProcessor(sagaStore, workflowStore, publisher);

        await processor.ProcessAsync(SampleEvent(invoiceNumber: "RS-90021", amount: 9500m));

        Assert.Equal(PaymentStatuses.Compensated, workflowStore.LastSaved!.Status);
        Assert.Equal(PaymentStatuses.Compensated, sagaStore.LastSaved!.Status);
        Assert.NotNull(sagaStore.LastSaved.ReservationId);
        Assert.NotNull(publisher.LastFailed);
        Assert.Equal(PaymentStatuses.Compensated, publisher.LastFailed!.Status);
    }

    [Fact]
    public async Task ProcessAsync_WithTerminalSagaState_DoesNotProcessAgain()
    {
        var sagaStore = new FakePaymentSagaStore();
        sagaStore.Seed(new PaymentSagaState(
            "tracking-1",
            "tracking-1",
            "correlation-1",
            100m,
            "INV-1001",
            PaymentSagaPhases.Completed,
            PaymentStatuses.Paid,
            "Already paid.",
            "res-tracking-1",
            DateTimeOffset.UtcNow));

        var workflowStore = new FakeSubmissionWorkflowStore();
        workflowStore.Seed(ApprovedState());
        var publisher = new FakePaymentOutcomePublisher();
        var processor = CreateProcessor(sagaStore, workflowStore, publisher);

        await processor.ProcessAsync(SampleEvent());

        var currentState = await workflowStore.GetAsync("tracking-1");
        Assert.NotNull(currentState);
        Assert.Equal(ApprovalOutcomeStatuses.AutoApproved, currentState!.Status);
        Assert.Null(publisher.LastCompleted);
        Assert.Null(publisher.LastFailed);
    }

    [Fact]
    public async Task ProcessAsync_WithNonApprovedStatus_DoesNotStartPayment()
    {
        var sagaStore = new FakePaymentSagaStore();
        var workflowStore = new FakeSubmissionWorkflowStore();
        workflowStore.Seed(ApprovedState(status: "Escalated"));
        var publisher = new FakePaymentOutcomePublisher();
        var processor = CreateProcessor(sagaStore, workflowStore, publisher);

        await processor.ProcessAsync(SampleEvent());

        var currentState = await workflowStore.GetAsync("tracking-1");
        Assert.NotNull(currentState);
        Assert.Equal("Escalated", currentState!.Status);
        Assert.Null(sagaStore.LastSaved);
        Assert.Null(publisher.LastCompleted);
    }

    private static PaymentSagaProcessor CreateProcessor(
        FakePaymentSagaStore sagaStore,
        FakeSubmissionWorkflowStore workflowStore,
        FakePaymentOutcomePublisher publisher)
    {
        var gateway = new StubPaymentGateway(
            Options.Create(new PaymentSimulationOptions()),
            NullLogger<StubPaymentGateway>.Instance);

        return new PaymentSagaProcessor(
            sagaStore,
            workflowStore,
            gateway,
            publisher,
            NullLogger<PaymentSagaProcessor>.Instance);
    }
}

public class StubPaymentGatewayTests
{
    [Fact]
    public async Task ExecuteAsync_WithRs90021_ReturnsFailure()
    {
        var gateway = new StubPaymentGateway(
            Options.Create(new PaymentSimulationOptions()),
            NullLogger<StubPaymentGateway>.Instance);

        var reservation = await gateway.ReserveAsync("tracking-1", "RS-90021", 9500m);
        var execution = await gateway.ExecuteAsync("tracking-1", "RS-90021", 9500m, reservation.ReservationId!);

        Assert.False(execution.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithRegularInvoice_ReturnsSuccess()
    {
        var gateway = new StubPaymentGateway(
            Options.Create(new PaymentSimulationOptions()),
            NullLogger<StubPaymentGateway>.Instance);

        var reservation = await gateway.ReserveAsync("tracking-1", "INV-1001", 100m);
        var execution = await gateway.ExecuteAsync("tracking-1", "INV-1001", 100m, reservation.ReservationId!);

        Assert.True(execution.Success);
    }
}
