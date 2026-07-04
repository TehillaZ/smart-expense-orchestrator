import { useState } from 'react'
import { SubmitExpenseForm } from '../components/submissions/SubmitExpenseForm'
import { SubmissionHistoryTable } from '../components/submissions/SubmissionHistoryTable'
import { MOCK_SUBMISSIONS } from '../data/mockSubmissions'
import type { ExpenseCategory, SubmissionRecord } from '../types/submission'

function createTrackingId(): string {
  const suffix = Math.random().toString(36).slice(2, 8).toUpperCase()
  return `TRK-${suffix}`
}

export function MySubmissionsPage() {
  const [submissions, setSubmissions] = useState<SubmissionRecord[]>(MOCK_SUBMISSIONS)

  function handleSubmitExpense(data: {
    vendorName: string
    invoiceNumber: string
    amount: number
    category: ExpenseCategory
  }) {
    const newSubmission: SubmissionRecord = {
      trackingId: createTrackingId(),
      vendor: data.vendorName,
      amount: data.amount,
      status: 'Pending',
      invoiceNumber: data.invoiceNumber,
      category: data.category,
    }

    setSubmissions((current) => [newSubmission, ...current])
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">My Submissions</h2>
        <p className="mt-1 text-sm text-slate-600">
          Submit new expenses and review your approval history in one place.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="lg:col-span-1">
          <SubmitExpenseForm onSubmit={handleSubmitExpense} />
        </div>
        <div className="lg:col-span-2">
          <SubmissionHistoryTable submissions={submissions} />
        </div>
      </div>
    </div>
  )
}
