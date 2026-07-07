import type { EscalationQueueItem } from '../types/api'
import type { SubmissionDetail, SubmissionRecord } from '../types/submission'

export function isInfoRequestedSubmission(
  item: Pick<SubmissionRecord | SubmissionDetail, 'humanDecision'>,
): boolean {
  return item.humanDecision === 'RequestInfo'
}

export function isInfoRequestedQueueItem(item: EscalationQueueItem): boolean {
  return item.humanDecision === 'RequestInfo'
}

export function getInfoRequestMessage(
  item: Pick<
    SubmissionRecord | SubmissionDetail | EscalationQueueItem,
    'humanDecision' | 'humanDecisionComment' | 'reason'
  >,
): string {
  if (item.humanDecisionComment?.trim()) {
    return item.humanDecisionComment.trim()
  }

  return item.reason
}
