import { API_ENDPOINTS } from '../config/api'
import type {
  ApprovalDecisionPayload,
  ApprovalDecisionResponse,
  EscalationQueueItem,
} from '../types/api'
import { getAuthSession } from './authService'
import { loadSubmissionDetail } from './submissionService'
import { requestJson } from './apiClient'

export async function loadEscalationQueue(): Promise<EscalationQueueItem[]> {
  return requestJson<EscalationQueueItem[]>(API_ENDPOINTS.approvalsQueue)
}

export async function loadEscalationItem(
  trackingId: string,
): Promise<EscalationQueueItem | null> {
  const queue = await loadEscalationQueue()
  const fromQueue = queue.find((item) => item.trackingId === trackingId)
  if (fromQueue) {
    return fromQueue
  }

  const detail = await loadSubmissionDetail(trackingId)
  if (!detail) {
    return null
  }

  return {
    trackingId: detail.trackingId,
    vendor: detail.vendor,
    invoiceNumber: detail.invoiceNumber,
    amount: detail.amount,
    category: detail.category,
    aiRecommendation: detail.aiRecommendation ?? 'Escalate',
    confidence: detail.aiConfidence ?? 0,
    citedRules: detail.citedRules ?? [],
    aiReasoningSummary: detail.aiReasoningSummary,
    reason: detail.reason,
    humanDecision: detail.humanDecision ?? null,
    humanDecisionComment: detail.humanDecisionComment ?? null,
    humanDecisionActor: detail.humanDecisionActor ?? null,
  }
}

export async function submitApprovalDecision(
  trackingId: string,
  payload: ApprovalDecisionPayload,
): Promise<ApprovalDecisionResponse> {
  const decidedBy = getAuthSession()?.username?.trim() || null

  return requestJson<ApprovalDecisionResponse>(API_ENDPOINTS.approvalDecision(trackingId), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      decision: payload.decision,
      comment: payload.comment ?? null,
      correlationId: null,
      decidedBy,
    }),
  })
}
