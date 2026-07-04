import { API_ENDPOINTS } from '../config/api'
import type {
  ApprovalDecisionPayload,
  ApprovalDecisionResponse,
  EscalationQueueItem,
} from '../types/api'
import { requestJson } from './apiClient'

export async function loadEscalationQueue(): Promise<EscalationQueueItem[]> {
  return requestJson<EscalationQueueItem[]>(API_ENDPOINTS.approvalsQueue)
}

export async function submitApprovalDecision(
  trackingId: string,
  payload: ApprovalDecisionPayload,
): Promise<ApprovalDecisionResponse> {
  return requestJson<ApprovalDecisionResponse>(API_ENDPOINTS.approvalDecision(trackingId), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      decision: payload.decision,
      comment: payload.comment ?? null,
      correlationId: null,
    }),
  })
}
