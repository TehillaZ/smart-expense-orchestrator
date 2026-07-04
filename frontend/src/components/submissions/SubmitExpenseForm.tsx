import { FormEvent, useState } from 'react'
import {
  EXPENSE_CATEGORIES,
  type ExpenseCategory,
  type SubmitExpenseFormData,
} from '../../types/submission'

const EMPTY_FORM: SubmitExpenseFormData = {
  vendorName: '',
  invoiceNumber: '',
  amount: '',
  category: '',
}

type FormErrors = Partial<Record<keyof SubmitExpenseFormData, string>>

type SubmitExpenseFormProps = {
  onSubmit: (data: {
    vendorName: string
    invoiceNumber: string
    amount: number
    category: ExpenseCategory
  }) => Promise<void>
}

function validateForm(data: SubmitExpenseFormData): FormErrors {
  const errors: FormErrors = {}

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

export function SubmitExpenseForm({ onSubmit }: SubmitExpenseFormProps) {
  const [form, setForm] = useState<SubmitExpenseFormData>(EMPTY_FORM)
  const [errors, setErrors] = useState<FormErrors>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  function updateField<K extends keyof SubmitExpenseFormData>(
    field: K,
    value: SubmitExpenseFormData[K],
  ) {
    setForm((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const validationErrors = validateForm(form)
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors)
      return
    }

    setIsSubmitting(true)

    try {
      await onSubmit({
        vendorName: form.vendorName.trim(),
        invoiceNumber: form.invoiceNumber.trim(),
        amount: Number.parseFloat(form.amount),
        category: form.category as ExpenseCategory,
      })

      setForm(EMPTY_FORM)
      setErrors({})
    } catch {
      // Parent shows the error toast; keep form values for retry.
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <div className="mb-6">
        <h2 className="text-lg font-semibold text-slate-900">Submit Expense</h2>
        <p className="mt-1 text-sm text-slate-500">
          Enter invoice details to start the approval workflow.
        </p>
      </div>

      <form className="space-y-4" onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="vendorName" className="mb-1.5 block text-sm font-medium text-slate-700">
            Vendor Name
          </label>
          <input
            id="vendorName"
            type="text"
            value={form.vendorName}
            onChange={(event) => updateField('vendorName', event.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm placeholder:text-slate-400 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            placeholder="Acme Supplies"
          />
          {errors.vendorName ? (
            <p className="mt-1 text-xs text-red-600">{errors.vendorName}</p>
          ) : null}
        </div>

        <div>
          <label
            htmlFor="invoiceNumber"
            className="mb-1.5 block text-sm font-medium text-slate-700"
          >
            Invoice Number
          </label>
          <input
            id="invoiceNumber"
            type="text"
            value={form.invoiceNumber}
            onChange={(event) => updateField('invoiceNumber', event.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm placeholder:text-slate-400 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            placeholder="INV-1024"
          />
          {errors.invoiceNumber ? (
            <p className="mt-1 text-xs text-red-600">{errors.invoiceNumber}</p>
          ) : null}
        </div>

        <div>
          <label htmlFor="amount" className="mb-1.5 block text-sm font-medium text-slate-700">
            Amount ($)
          </label>
          <input
            id="amount"
            type="number"
            min="0"
            step="0.01"
            value={form.amount}
            onChange={(event) => updateField('amount', event.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm placeholder:text-slate-400 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            placeholder="0.00"
          />
          {errors.amount ? <p className="mt-1 text-xs text-red-600">{errors.amount}</p> : null}
        </div>

        <div>
          <label htmlFor="category" className="mb-1.5 block text-sm font-medium text-slate-700">
            Category
          </label>
          <select
            id="category"
            value={form.category}
            onChange={(event) =>
              updateField('category', event.target.value as ExpenseCategory | '')
            }
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
          >
            <option value="">Select category</option>
            {EXPENSE_CATEGORIES.map((category) => (
              <option key={category} value={category}>
                {category}
              </option>
            ))}
          </select>
          {errors.category ? (
            <p className="mt-1 text-xs text-red-600">{errors.category}</p>
          ) : null}
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-300 focus:ring-offset-2 disabled:cursor-not-allowed disabled:bg-blue-400"
        >
          {isSubmitting ? 'Submitting…' : 'Submit Expense'}
        </button>
      </form>
    </section>
  )
}
