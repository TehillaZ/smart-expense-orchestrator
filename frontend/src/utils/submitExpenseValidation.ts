import type { SubmitExpenseFormData } from '../types/submission'

export type SubmitExpenseFormErrors = Partial<Record<keyof SubmitExpenseFormData, string>>

export function validateSubmitExpenseForm(
  data: SubmitExpenseFormData,
): SubmitExpenseFormErrors {
  const errors: SubmitExpenseFormErrors = {}

  if (!data.vendorName.trim()) {
    errors.vendorName = 'Vendor name is required.'
  }

  if (!data.invoiceNumber.trim()) {
    errors.invoiceNumber = 'Invoice number is required.'
  }

  const amount = Number.parseFloat(data.amount)
  if (!data.amount.trim() || Number.isNaN(amount) || amount <= 0) {
    errors.amount = 'Enter a valid amount greater than zero.'
  }

  if (!data.category) {
    errors.category = 'Select a category.'
  }

  return errors
}

export function hasSubmitExpenseFormErrors(errors: SubmitExpenseFormErrors): boolean {
  return Object.keys(errors).length > 0
}
