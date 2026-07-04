import { useCallback, useEffect, useState } from 'react'
import { SubmitExpenseForm } from '../components/submissions/SubmitExpenseForm'
import { SubmissionHistoryTable } from '../components/submissions/SubmissionHistoryTable'
import { Toast } from '../components/ui/Toast'
import { useAppRole } from '../hooks/useAppRole'
import { ApiRequestError } from '../services/apiClient'
import { loadSubmissionHistory, submitExpense } from '../services/submissionService'
import type { ExpenseCategory, SubmissionRecord } from '../types/submission'
import { canSubmitExpenses } from '../types/userRole'

const REFRESH_INTERVAL_MS = 5000

type ToastState = {
  message: string
  variant: 'success' | 'error'
}

export function MySubmissionsPage() {
  const role = useAppRole()
  const showSubmitForm = canSubmitExpenses(role)
  const [submissions, setSubmissions] = useState<SubmissionRecord[]>([])
  const [isLoadingHistory, setIsLoadingHistory] = useState(true)
  const [historyError, setHistoryError] = useState<string | null>(null)
  const [toast, setToast] = useState<ToastState | null>(null)

  const refreshHistory = useCallback(async (showLoading = false) => {
    if (showLoading) {
      setIsLoadingHistory(true)
    }

    setHistoryError(null)

    try {
      const records = await loadSubmissionHistory()
      setSubmissions(records)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Failed to load submission history.'
      setHistoryError(message)
    } finally {
      setIsLoadingHistory(false)
    }
  }, [])

  useEffect(() => {
    void refreshHistory(true)

    const intervalId = window.setInterval(() => {
      void refreshHistory(false)
    }, REFRESH_INTERVAL_MS)

    return () => window.clearInterval(intervalId)
  }, [refreshHistory])

  useEffect(() => {
    if (!toast) {
      return undefined
    }

    const timeoutId = window.setTimeout(() => setToast(null), 4000)
    return () => window.clearTimeout(timeoutId)
  }, [toast])

  async function handleSubmitExpense(data: {
    vendorName: string
    invoiceNumber: string
    amount: number
    category: ExpenseCategory
  }) {
    try {
      const response = await submitExpense(data)
      const duplicateNote = response.isDuplicate ? ' (existing submission returned)' : ''

      setToast({
        message: `Submission accepted. Tracking ID: ${response.trackingId}${duplicateNote}`,
        variant: 'success',
      })

      await refreshHistory(false)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? (error.details?.join(' ') ?? error.message)
          : 'Failed to submit expense. Please try again.'

      setToast({
        message,
        variant: 'error',
      })

      throw error
    }
  }

  return (
    <>
      {toast ? (
        <Toast
          message={toast.message}
          variant={toast.variant}
          onDismiss={() => setToast(null)}
        />
      ) : null}

      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-slate-900">My Submissions</h2>
          <p className="mt-1 text-sm text-slate-600">
            {showSubmitForm
              ? 'Submit new expenses and review live approval outcomes. Amounts over $400 escalate to the Approvals Queue; lower amounts auto-approve when policy allows.'
              : 'Review submission history across the platform.'}
          </p>
        </div>

        <div
          className={
            showSubmitForm
              ? 'grid grid-cols-1 gap-6 lg:grid-cols-3'
              : 'grid grid-cols-1 gap-6'
          }
        >
          {showSubmitForm ? (
            <div className="lg:col-span-1">
              <SubmitExpenseForm onSubmit={handleSubmitExpense} />
            </div>
          ) : null}
          <div className={showSubmitForm ? 'lg:col-span-2' : undefined}>
          <SubmissionHistoryTable
            submissions={submissions}
            isLoading={isLoadingHistory}
            errorMessage={historyError}
            onRetry={() => void refreshHistory(true)}
          />
        </div>
        </div>
      </div>
    </>
  )
}
