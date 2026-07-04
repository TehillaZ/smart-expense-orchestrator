export const SUBMISSION_STATUSES = ['Approved', 'Rejected', 'Pending'] as const

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

export type SubmissionRecord = {
  trackingId: string
  vendor: string
  amount: number
  status: SubmissionStatus
  invoiceNumber: string
  category: ExpenseCategory
}

export type SubmitExpenseFormData = {
  vendorName: string
  invoiceNumber: string
  amount: string
  category: ExpenseCategory | ''
}
