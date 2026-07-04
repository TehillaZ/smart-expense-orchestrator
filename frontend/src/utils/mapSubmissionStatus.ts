import type { SubmissionStatus } from '../types/submission'

const APPROVED_STATUSES = new Set(['AutoApproved', 'HumanApproved', 'Paid'])
const REJECTED_STATUSES = new Set(['Rejected', 'PaymentFailed', 'Compensated'])
const ESCALATED_STATUSES = new Set(['Escalated'])

export function mapBackendStatusToUi(status: string): SubmissionStatus {
  if (APPROVED_STATUSES.has(status)) {
    return 'Approved'
  }

  if (REJECTED_STATUSES.has(status)) {
    return 'Rejected'
  }

  if (ESCALATED_STATUSES.has(status)) {
    return 'Escalated'
  }

  return 'Pending'
}
