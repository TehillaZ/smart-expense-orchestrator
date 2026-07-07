namespace IntakeService.Models;

public sealed record AuditInvoiceSnapshot(
    string TrackingId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category);

public sealed record AuditAiSnapshot(
    string? Recommendation,
    decimal? Confidence,
    IReadOnlyList<string> CitedRules,
    string ReasoningSummary);

public sealed record AuditHumanDecisionSnapshot(
    string Decision,
    string? Comment,
    string Actor);

public sealed record AuditPaymentSnapshot(
    string Status,
    string Reason,
    string? OutcomeType,
    string? Channel,
    DateTimeOffset? NotifiedAt);

public sealed record AuditTimelineEvent(
    string Step,
    string Summary,
    string Actor,
    string? Detail = null);

public sealed record AuditTrailResponse(
    string TrackingId,
    string CorrelationId,
    AuditInvoiceSnapshot Invoice,
    string PolicyVersion,
    IReadOnlyList<string> RulesApplied,
    AuditAiSnapshot Ai,
    AuditHumanDecisionSnapshot? HumanDecision,
    AuditPaymentSnapshot? Payment,
    IReadOnlyList<AuditTimelineEvent> Timeline);

public sealed record RecentActivityItem(
    string TrackingId,
    string Status,
    decimal Amount,
    string DecisionType,
    string Vendor,
    string InvoiceNumber,
    DateTimeOffset RecordedAt);

public sealed record ReportingSummaryResponse(
    int TotalSubmissions,
    int AutoApprovedCount,
    int EscalatedCount,
    int HumanApprovedCount,
    int RejectedCount,
    int PaidCount,
    int PaymentFailedCount,
    decimal AutoApprovedAmount,
    decimal HumanApprovedAmount,
    decimal AutoApprovalRate,
    decimal EscalationRate,
    int SubmissionsTodayCount,
    IReadOnlyList<RecentActivityItem> RecentActivity);

public sealed record AutoApprovalComplianceItem(
    string TrackingId,
    string InvoiceNumber,
    string Vendor,
    decimal Amount,
    bool WithinCeiling,
    bool IsViolation);

public sealed record ComplianceReportResponse(
    decimal AutonomyCeiling,
    string PolicyVersion,
    int AutoApprovedReviewedCount,
    int ViolationCount,
    IReadOnlyList<AutoApprovalComplianceItem> AutoApprovedItems);

public sealed record NotificationLookupResponse(
    string TrackingId,
    string CorrelationId,
    string Status,
    string Reason,
    decimal Amount,
    string OutcomeType,
    string Channel,
    DateTimeOffset SentAt);
