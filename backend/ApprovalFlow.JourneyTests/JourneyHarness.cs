using ApprovalService.Models;
using ApprovalService.Services;
using IntakeService.Models;
using IntakeService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PaymentService.Models;
using PaymentService.Services;
using ApprovalSubmissionReceivedEvent = ApprovalService.Models.SubmissionReceivedEvent;
using PaymentRequestedEvent = PaymentService.Models.PaymentRequestedEvent;

namespace ApprovalFlow.JourneyTests;

internal sealed class JourneyHarness
{
    private readonly InMemorySubmissionStore intakeStore;
    private readonly JourneyApprovalStateStore approvalStore;
    private readonly ApprovalWorkflowProcessor approvalProcessor;
    private readonly ApprovalDecisionHandler decisionHandler;
    private readonly JourneyPaymentRequestPublisher paymentPublisher;
    private readonly JourneyPaymentOutcomePublisher outcomePublisher;

    private JourneyHarness(
        InMemorySubmissionStore intakeStore,
        JourneyApprovalStateStore approvalStore,
        ApprovalWorkflowProcessor approvalProcessor,
        ApprovalDecisionHandler decisionHandler,
        JourneyPaymentRequestPublisher paymentPublisher,
        JourneyPaymentOutcomePublisher outcomePublisher)
    {
        this.intakeStore = intakeStore;
        this.approvalStore = approvalStore;
        this.approvalProcessor = approvalProcessor;
        this.decisionHandler = decisionHandler;
        this.paymentPublisher = paymentPublisher;
        this.outcomePublisher = outcomePublisher;
    }

    public int PaymentPublishCount => paymentPublisher.PublishCount;

    public static JourneyHarness Create()
    {
        var intakeStore = new InMemorySubmissionStore();
        var approvalStore = new JourneyApprovalStateStore();
        var policyStore = new JourneyPolicySettingsStore();
        var sagaStore = new JourneyPaymentSagaStore();
        var outcomePublisher = new JourneyPaymentOutcomePublisher();
        var workflowStore = new SyncedSubmissionWorkflowStore(approvalStore);

        var paymentGateway = new StubPaymentGateway(
            Options.Create(new PaymentSimulationOptions()),
            NullLogger<StubPaymentGateway>.Instance);

        var paymentProcessor = new PaymentSagaProcessor(
            sagaStore,
            workflowStore,
            paymentGateway,
            outcomePublisher,
            NullLogger<PaymentSagaProcessor>.Instance);

        var paymentPublisher = new JourneyPaymentRequestPublisher(paymentProcessor);

        var approvalProcessor = new ApprovalWorkflowProcessor(
            approvalStore,
            policyStore,
            new StubInvoiceAiProvider(),
            new PolicyEngine(),
            paymentPublisher,
            NullLogger<ApprovalWorkflowProcessor>.Instance);

        var decisionHandler = new ApprovalDecisionHandler(
            approvalStore,
            paymentPublisher,
            NullLogger<ApprovalDecisionHandler>.Instance);

        return new JourneyHarness(
            intakeStore,
            approvalStore,
            approvalProcessor,
            decisionHandler,
            paymentPublisher,
            outcomePublisher);
    }

    public async Task<SubmissionCreationResult> SubmitAsync(
        string vendor,
        string invoiceNumber,
        decimal amount,
        string category)
    {
        var correlationId = Guid.NewGuid().ToString();
        var request = new SubmissionRequest(vendor, invoiceNumber, amount, category);
        var creation = await intakeStore.CreateSubmissionAsync(request, correlationId);

        if (!creation.IsDuplicate)
        {
            var submission = creation.Submission;
            await approvalProcessor.ProcessAsync(
                new ApprovalSubmissionReceivedEvent(
                    submission.TrackingId,
                    submission.CorrelationId,
                    submission.Vendor,
                    submission.InvoiceNumber,
                    submission.Amount,
                    submission.Category));
        }

        return creation;
    }

    public async Task<(ApprovalDecisionResponse? Response, IReadOnlyList<string> Errors)> ApproveAsync(string trackingId)
    {
        return await decisionHandler.ApplyDecisionAsync(
            trackingId,
            new ApprovalDecisionRequest("Approve", "Approved during journey test.", null));
    }

    public async Task<SubmissionState?> GetWorkflowStateAsync(string trackingId)
    {
        return await approvalStore.GetAsync(trackingId);
    }

    public PaymentCompletedEvent? LastCompleted => outcomePublisher.LastCompleted;

    public PaymentFailedEvent? LastFailed => outcomePublisher.LastFailed;
}

internal sealed class JourneyApprovalStateStore : ISubmissionStateStore
{
    private readonly Dictionary<string, SubmissionState> states = new(StringComparer.Ordinal);
    private readonly HashSet<string> escalationQueue = new(StringComparer.Ordinal);

    public Task<SubmissionState?> GetAsync(string trackingId)
    {
        states.TryGetValue(trackingId, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(SubmissionState state)
    {
        states[state.TrackingId] = state;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SubmissionState>> GetEscalatedSubmissionsAsync()
    {
        IReadOnlyList<SubmissionState> results = escalationQueue
            .Select(id => states.TryGetValue(id, out var state) ? state : null)
            .Where(state => state is not null && state.Status == ApprovalStatuses.Escalated)
            .Cast<SubmissionState>()
            .ToArray();

        return Task.FromResult(results);
    }

    public Task AddToEscalationQueueAsync(string trackingId)
    {
        escalationQueue.Add(trackingId);
        return Task.CompletedTask;
    }

    public Task RemoveFromEscalationQueueAsync(string trackingId)
    {
        escalationQueue.Remove(trackingId);
        return Task.CompletedTask;
    }
}

internal sealed class JourneyPolicySettingsStore : IPolicySettingsStore
{
    public Task<PolicySettings> GetAsync() => Task.FromResult(PolicySettings.Default);

    public Task SaveAsync(PolicySettings settings) => Task.CompletedTask;
}

internal sealed class JourneyPaymentSagaStore : IPaymentSagaStore
{
    private readonly Dictionary<string, PaymentSagaState> states = new(StringComparer.Ordinal);

    public Task<PaymentSagaState?> GetAsync(string idempotencyKey)
    {
        states.TryGetValue(idempotencyKey, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(PaymentSagaState state)
    {
        states[state.IdempotencyKey] = state;
        return Task.CompletedTask;
    }
}

internal sealed class JourneyPaymentOutcomePublisher : IPaymentOutcomePublisher
{
    public PaymentCompletedEvent? LastCompleted { get; private set; }

    public PaymentFailedEvent? LastFailed { get; private set; }

    public Task PublishCompletedAsync(PaymentCompletedEvent paymentEvent)
    {
        LastCompleted = paymentEvent;
        return Task.CompletedTask;
    }

    public Task PublishFailedAsync(PaymentFailedEvent paymentEvent)
    {
        LastFailed = paymentEvent;
        return Task.CompletedTask;
    }
}

internal sealed class JourneyPaymentRequestPublisher : IPaymentRequestPublisher
{
    private readonly PaymentSagaProcessor paymentProcessor;

    public JourneyPaymentRequestPublisher(PaymentSagaProcessor paymentProcessor)
    {
        this.paymentProcessor = paymentProcessor;
    }

    public int PublishCount { get; private set; }

    public async Task PublishAsync(SubmissionState state)
    {
        PublishCount++;

        var paymentEvent = new PaymentRequestedEvent(
            state.TrackingId,
            state.CorrelationId,
            state.Vendor,
            state.InvoiceNumber,
            state.Amount,
            state.Category,
            ApprovalService.Services.SubmissionIdempotencyKeys.Build(state.Vendor, state.InvoiceNumber, state.Amount));

        await paymentProcessor.ProcessAsync(paymentEvent);
    }
}

internal sealed class SyncedSubmissionWorkflowStore : ISubmissionWorkflowStore
{
    private readonly JourneyApprovalStateStore approvalStore;

    public SyncedSubmissionWorkflowStore(JourneyApprovalStateStore approvalStore)
    {
        this.approvalStore = approvalStore;
    }

    public async Task<SubmissionWorkflowState?> GetAsync(string trackingId)
    {
        var state = await approvalStore.GetAsync(trackingId);
        return state is null ? null : ToWorkflowState(state);
    }

    public async Task SaveAsync(SubmissionWorkflowState state)
    {
        var existing = await approvalStore.GetAsync(state.TrackingId);
        if (existing is null)
        {
            return;
        }

        await approvalStore.SaveAsync(existing with
        {
            Status = state.Status,
            Reason = state.Reason,
            AiRecommendation = state.AiRecommendation,
            AiConfidence = state.AiConfidence,
            CitedRules = state.CitedRules,
            PolicyVersion = state.PolicyVersion
        });
    }

    private static SubmissionWorkflowState ToWorkflowState(SubmissionState state) =>
        new(
            state.TrackingId,
            state.CorrelationId,
            state.Vendor,
            state.InvoiceNumber,
            state.Amount,
            state.Category,
            state.Status,
            state.Reason,
            state.AiRecommendation,
            state.AiConfidence,
            state.CitedRules,
            state.PolicyVersion);
}
