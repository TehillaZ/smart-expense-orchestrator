import { useCallback, useEffect, useState } from 'react'
import { SubmitExpenseForm } from '../components/submissions/SubmitExpenseForm'
import { SubmissionDetailPanel } from '../components/submissions/SubmissionDetailPanel'
import { SubmissionHistoryTable } from '../components/submissions/SubmissionHistoryTable'
import { SubmitSuccessPanel } from '../components/submissions/SubmitSuccessPanel'
import { TrackSubmissionSearch } from '../components/submissions/TrackSubmissionSearch'
import { Toast } from '../components/ui/Toast'
import { useAppRole } from '../hooks/useAppRole'
import { ApiRequestError } from '../services/apiClient'
import {
  loadSubmissionDetail,
  loadSubmissionHistory,
  submitExpense,
} from '../services/submissionService'
import type { ExpenseCategory, SubmissionDetail, SubmissionRecord } from '../types/submission'
import { canSubmitExpenses } from '../types/userRole'

const REFRESH_INTERVAL_MS = 5000

type ToastState = {
  message: string
  variant: 'success' | 'error'
}

type SubmitSuccessState = {
  trackingId: string
  isDuplicate: boolean
}

type MySubmissionsPageProps = {
  mode?: 'submit' | 'track'
}

export function MySubmissionsPage({ mode = 'submit' }: MySubmissionsPageProps) {
  const role = useAppRole()
  const showSubmitForm = canSubmitExpenses(role) && mode === 'submit'
  const [submissions, setSubmissions] = useState<SubmissionRecord[]>([])
  const [selectedTrackingId, setSelectedTrackingId] = useState<string | null>(null)
  const [selectedDetail, setSelectedDetail] = useState<SubmissionDetail | null>(null)
  const [submitSuccess, setSubmitSuccess] = useState<SubmitSuccessState | null>(null)
  const [isLoadingHistory, setIsLoadingHistory] = useState(true)
  const [isLoadingDetail, setIsLoadingDetail] = useState(false)
  const [historyError, setHistoryError] = useState<string | null>(null)
  const [detailError, setDetailError] = useState<string | null>(null)
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

  const loadDetail = useCallback(async (trackingId: string) => {
    setIsLoadingDetail(true)
    setDetailError(null)
    setSelectedTrackingId(trackingId)

    try {
      const detail = await loadSubmissionDetail(trackingId)

      if (!detail) {
        setSelectedDetail(null)
        setDetailError('Submission was not found.')
        return
      }

      setSelectedDetail(detail)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Failed to load submission status.'
      setDetailError(message)
      setSelectedDetail(null)
    } finally {
      setIsLoadingDetail(false)
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

      setSubmitSuccess({
        trackingId: response.trackingId,
        isDuplicate: response.isDuplicate,
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

  function handleViewSuccessStatus() {
    if (!submitSuccess) {
      return
    }

    void loadDetail(submitSuccess.trackingId)
    setSubmitSuccess(null)
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
          <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
            {mode === 'track' ? 'Track Submissions' : 'My Submissions'}
          </h2>
          <p className="mt-1 text-sm text-slate-600">
            {mode === 'track'
              ? 'Search by tracking id and review plain-language approval outcomes.'
              : showSubmitForm
                ? 'Submit new expenses and track plain-language approval outcomes.'
                : 'Review submission history and status details across the platform.'}
          </p>
        </div>

        {submitSuccess ? (
          <SubmitSuccessPanel
            trackingId={submitSuccess.trackingId}
            isDuplicate={submitSuccess.isDuplicate}
            onViewStatus={handleViewSuccessStatus}
            onDismiss={() => setSubmitSuccess(null)}
          />
        ) : null}

        <TrackSubmissionSearch
          isLoading={isLoadingDetail}
          onSearch={(trackingId) => void loadDetail(trackingId)}
        />

        {detailError ? (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            {detailError}
          </div>
        ) : null}

        {selectedDetail ? (
          <SubmissionDetailPanel
            detail={selectedDetail}
            isDuplicateResubmit={selectedDetail.isDuplicateResubmit}
            isRefreshing={isLoadingDetail}
            onRefresh={() => {
              if (selectedTrackingId) {
                void loadDetail(selectedTrackingId)
              }
            }}
            onClose={() => {
              setSelectedDetail(null)
              setSelectedTrackingId(null)
            }}
          />
        ) : null}

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
              selectedTrackingId={selectedTrackingId}
              onRetry={() => void refreshHistory(true)}
              onSelect={(trackingId) => void loadDetail(trackingId)}
            />
          </div>
        </div>
      </div>
    </>
  )
}
