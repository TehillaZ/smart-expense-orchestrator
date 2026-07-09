using ApprovalService.Models;
using ApprovalService.Services;
using IntakeService.Models;
using IntakeService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NotificationService.Models;
using NotificationService.Services;
using PaymentService.Models;
using PaymentService.Services;
using ApprovalSubmissionReceivedEvent = ApprovalService.Models.SubmissionReceivedEvent;
using PaymentRequestedEvent = PaymentService.Models.PaymentRequestedEvent;
using PaymentCompletedEvent = PaymentService.Models.PaymentCompletedEvent;
using PaymentFailedEvent = PaymentService.Models.PaymentFailedEvent;

namespace ApprovalFlow.JourneyTests;

internal sealed class JourneyHarness
{
    private readonly InMemorySubmissionStore intakeStore;
    private readonly JourneyApprovalStateStore approvalStore;
    private readonly ApprovalWorkflowProcessor approvalProcessor;
    private readonly ApprovalDecisionHandler decisionHandler;
    private readonly JourneyPaymentRequestPublisher paymentPublisher;
    private readonly JourneyPaymentOutcomePublisher outcomePublisher;
    private readonly InMemoryNotificationStore notificationStore;

    private JourneyHarness(
        InMemorySubmissionStore intakeStore,
        JourneyApprovalStateStore approvalStore,
        ApprovalWorkflowProcessor approvalProcessor,
        ApprovalDecisionHandler decisionHandler,
        JourneyPaymentRequestPublisher paymentPublisher,
        JourneyPaymentOutcomePublisher outcomePublisher,
        InMemoryNotificationStore notificationStore)
    {
        this.intakeStore = intakeStore;
        this.approvalStore = approvalStore;
        this.approvalProcessor = approvalProcessor;
        this.decisionHandler = decisionHandler;
        this.paymentPublisher = paymentPublisher;
        this.outcomePublisher = outcomePublisher;
        this.notificationStore = notificationStore;
    }

    public int PaymentPublishCount => paymentPublisher.PublishCount;

    public static JourneyHarness Create(IInvoiceAiProvider? aiProvider = null) =>
        CreateCore(aiProvider ?? JourneyPolicyRagHelper.CreateStubProvider());

    private static JourneyHarness CreateCore(IInvoiceAiProvider aiProvider)
    {
        var intakeStore = new InMemorySubmissionStore();
        var approvalStore = new JourneyApprovalStateStore();
        var policyStore = new JourneyPolicySettingsStore();
        var sagaStore = new JourneyPaymentSagaStore();
        var notificationStore = new InMemoryNotificationStore();
        var notificationProcessor = new NotificationProcessor(
            notificationStore,
            new JourneyNotificationChannel(),
            NullLogger<NotificationProcessor>.Instance);
        var outcomePublisher = new JourneyPaymentOutcomePublisher(notificationProcessor);
        var workflowStore = new SyncedSubmissionWorkflowStore(approvalStore);

        var paymentGateway = new StubPaymentGateway(
            Options.Create(new PaymentSimulationOptions()),
            NullLogger<StubPaymentGateway>.Instance);

        var paymentProcessor = new PaymentSagaProcessor(
            sagaStore,
            workflowStore,
            paymentGateway,
            outcomePublisher,
            new NoOpPaymentMetricsRecorder(),
            NullLogger<PaymentSagaProcessor>.Instance);

        var paymentPublisher = new JourneyPaymentRequestPublisher(paymentProcessor);

        var approvalProcessor = new ApprovalWorkflowProcessor(
            approvalStore,
            policyStore,
            aiProvider,
            new PolicyEngine(),
            paymentPublisher,
            new NoOpApprovalMetricsRecorder(),
            NullLogger<ApprovalWorkflowProcessor>.Instance);

        var decisionHandler = new ApprovalDecisionHandler(
            approvalStore,
            paymentPublisher,
            new NoOpApprovalMetricsRecorder(),
            NullLogger<ApprovalDecisionHandler>.Instance);

        return new JourneyHarness(
            intakeStore,
            approvalStore,
            approvalProcessor,
            decisionHandler,
            paymentPublisher,
            outcomePublisher,
            notificationStore);
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

    public Task<(ApprovalDecisionResponse? Response, IReadOnlyList<string> Errors)> ApproveAfterSimulatedServiceRestartAsync(
        string trackingId)
    {
        var restartedDecisionHandler = new ApprovalDecisionHandler(
            approvalStore,
            paymentPublisher,
            new NoOpApprovalMetricsRecorder(),
            NullLogger<ApprovalDecisionHandler>.Instance);

        return restartedDecisionHandler.ApplyDecisionAsync(
            trackingId,
            new ApprovalDecisionRequest(
                "Approve",
                "Approved after simulated Approval Service restart.",
                null));
    }

    public async Task<SubmissionCreationResult[]> SubmitConcurrentlyAsync(
        int submissionCount,
        string vendor,
        string invoiceNumber,
        decimal amount,
        string category)
    {
        var submissions = Enumerable.Range(0, submissionCount)
            .Select(_ => SubmitAsync(vendor, invoiceNumber, amount, category))
            .ToArray();

        return await Task.WhenAll(submissions);
    }

    public async Task<SubmissionState?> GetWorkflowStateAsync(string trackingId)
    {
        return await approvalStore.GetAsync(trackingId);
    }

    public PaymentCompletedEvent? LastCompleted => outcomePublisher.LastCompleted;

    public PaymentFailedEvent? LastFailed => outcomePublisher.LastFailed;

    public async Task<NotificationRecord?> GetNotificationAsync(string trackingId)
    {
        var completed = await notificationStore.GetAsync(
            trackingId,
            NotificationOutcomeTypes.PaymentCompleted);
        if (completed is not null)
        {
            return completed;
        }

        return await notificationStore.GetAsync(
            trackingId,
            NotificationOutcomeTypes.PaymentFailed);
    }
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
    private readonly NotificationProcessor notificationProcessor;

    public JourneyPaymentOutcomePublisher(NotificationProcessor notificationProcessor)
    {
        this.notificationProcessor = notificationProcessor;
    }

    public PaymentCompletedEvent? LastCompleted { get; private set; }

    public PaymentFailedEvent? LastFailed { get; private set; }

    public async Task PublishCompletedAsync(PaymentCompletedEvent paymentEvent)
    {
        LastCompleted = paymentEvent;
        await notificationProcessor.ProcessPaymentCompletedAsync(
            new NotificationService.Models.PaymentCompletedEvent(
                paymentEvent.TrackingId,
                paymentEvent.CorrelationId,
                paymentEvent.Status,
                paymentEvent.Reason,
                paymentEvent.Amount));
    }

    public async Task PublishFailedAsync(PaymentFailedEvent paymentEvent)
    {
        LastFailed = paymentEvent;
        await notificationProcessor.ProcessPaymentFailedAsync(
            new NotificationService.Models.PaymentFailedEvent(
                paymentEvent.TrackingId,
                paymentEvent.CorrelationId,
                paymentEvent.Status,
                paymentEvent.Reason,
                paymentEvent.Amount));
    }
}

internal sealed class JourneyNotificationChannel : INotificationChannel
{
    public Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
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
