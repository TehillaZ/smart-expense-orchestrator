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

export type ApiErrorBody = {
  message?: string
  errors?: string[]
}
