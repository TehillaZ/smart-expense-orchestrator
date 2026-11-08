export const SUBMISSION_STATUSES = ['Approved', 'Rejected', 'Pending', 'Escalated'] as const

export type SubmissionStatus = (typeof SUBMISSION_STATUSES)[number]

export const EXPENSE_CATEGORIES = [
  'Travel',
  'Software',
  'Office Supplies',
  'Meals',
  'Equipment',
  'Other',
] as const

export type ExpenseCategory = (typeof EXPENSE_CATEGORIES)[number]

/**
 * The backend policy engine and RAG retriever only recognize
 * travel/saas/hardware/meals (see ApprovalService PolicySettings.Default).
 * Categories with no dedicated backend rule (Office Supplies, Other) are
 * sent through unchanged and fall back to the generic policy path.
 */
const CATEGORY_TO_BACKEND: Partial<Record<ExpenseCategory, string>> = {
  Travel: 'travel',
  Software: 'saas',
  Meals: 'meals',
  Equipment: 'hardware',
}

export function toBackendCategory(category: ExpenseCategory): string {
  return CATEGORY_TO_BACKEND[category] ?? category
}

export type SubmissionRecord = {
  trackingId: string
  vendor: string
  amount: number
  status: SubmissionStatus
  invoiceNumber: string
  category: ExpenseCategory
  backendStatus?: string
  reason?: string
  isDuplicateResubmit?: boolean
  duplicateResubmitCount?: number
  humanDecision?: string | null
  humanDecisionComment?: string | null
  humanDecisionActor?: string | null
}

export type SubmissionDetail = SubmissionRecord & {
  correlationId: string
  reason: string
  notificationReason?: string | null
  aiRecommendation?: string | null
  aiConfidence?: number | null
  citedRules?: string[]
  aiReasoningSummary?: string | null
  policyVersion?: string | null
  autonomyCeilingAtDecision?: number | null
}

export type SubmitExpenseFormData = {
  vendorName: string
  invoiceNumber: string
  amount: string
  category: ExpenseCategory | ''
}
