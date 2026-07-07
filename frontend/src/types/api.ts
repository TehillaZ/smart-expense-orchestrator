export type SubmissionRequestPayload = {
  vendor: string
  invoiceNumber: string
  amount: number
  category: string
}

export type SubmissionAcceptedResponse = {
  trackingId: string
  correlationId: string
  isDuplicate: boolean
}

export type SubmissionStatusResponse = {
  trackingId: string
  correlationId: string
  vendor: string
  invoiceNumber: string
  amount: number
  category: string
  status: string
  reason: string
  aiRecommendation?: string | null
  aiConfidence?: number | null
  citedRules?: string[] | null
  policyVersion?: string | null
  humanDecision?: string | null
  humanDecisionComment?: string | null
  duplicateResubmitCount?: number
}

export type ValidationErrorResponse = {
  errors: string[]
}

export type NotificationResponse = {
  trackingId: string
  correlationId: string
  status: string
  reason: string
  amount: number
  outcomeType: string
  channel: string
  sentAt: string
}

export type EscalationQueueItem = {
  trackingId: string
  vendor: string
  invoiceNumber: string
  amount: number
  category: string
  aiRecommendation: string
  confidence: number
  citedRules: string[]
  reason: string
  humanDecision?: string | null
  humanDecisionComment?: string | null
}

export type ApprovalDecision = 'Approve' | 'Reject' | 'RequestInfo'

export type ApprovalDecisionPayload = {
  decision: ApprovalDecision
  comment?: string
}

export type ApprovalDecisionResponse = {
  trackingId: string
  status: string
  correlationId: string
  reason: string
}

export type PolicySettings = {
  autonomyCeiling: number
  autonomyConfidence: number
  policyVersion: string
}

export type PolicySettingsUpdatePayload = {
  autonomyCeiling?: number
  autonomyConfidence?: number
}

export type AuditTimelineEvent = {
  step: string
  summary: string
  actor: string
  detail?: string | null
}

export type AuditTrailResponse = {
  trackingId: string
  correlationId: string
  invoice: {
    trackingId: string
    vendor: string
    invoiceNumber: string
    amount: number
    category: string
  }
  policyVersion: string
  rulesApplied: string[]
  ai: {
    recommendation?: string | null
    confidence?: number | null
    citedRules: string[]
    reasoningSummary: string
  }
  humanDecision?: {
    decision: string
    comment?: string | null
    actor: string
  } | null
  payment?: {
    status: string
    reason: string
    outcomeType?: string | null
    channel?: string | null
    notifiedAt?: string | null
  } | null
  timeline: AuditTimelineEvent[]
}

export type RecentActivityItem = {
  trackingId: string
  status: string
  amount: number
  decisionType: string
  vendor: string
  invoiceNumber: string
  recordedAt?: string | null
}

export type ReportingSummaryResponse = {
  totalSubmissions: number
  autoApprovedCount: number
  escalatedCount: number
  humanApprovedCount: number
  rejectedCount: number
  paidCount: number
  paymentFailedCount: number
  autoApprovedAmount: number
  humanApprovedAmount: number
  autoApprovalRate: number
  escalationRate: number
  submissionsTodayCount: number
  recentActivity: RecentActivityItem[]
}

export type AutoApprovalComplianceItem = {
  trackingId: string
  invoiceNumber: string
  vendor: string
  amount: number
  withinCeiling: boolean
  isViolation: boolean
}

export type ComplianceReportResponse = {
  autonomyCeiling: number
  policyVersion: string
  autoApprovedReviewedCount: number
  violationCount: number
  autoApprovedItems: AutoApprovalComplianceItem[]
}

export type ApiErrorBody = {
  message?: string
  errors?: string[]
}
