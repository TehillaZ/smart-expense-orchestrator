import { useCallback, useEffect, useState } from 'react'
import { EscalationQueueTable } from '../components/approvals/EscalationQueueTable'
import { Toast } from '../components/ui/Toast'
import { useAppRole } from '../hooks/useAppRole'
import { ApiRequestError } from '../services/apiClient'
import { loadEscalationQueue, submitApprovalDecision } from '../services/approvalService'
import type { ApprovalDecision, EscalationQueueItem } from '../types/api'
import { canDecideEscalations } from '../types/userRole'

const REFRESH_INTERVAL_MS = 5000

type ToastState = {
  message: string
  variant: 'success' | 'error'
}

export function ApprovalsQueuePage() {
  const role = useAppRole()
  const canDecide = canDecideEscalations(role)
  const [items, setItems] = useState<EscalationQueueItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [decidingTrackingId, setDecidingTrackingId] = useState<string | null>(null)
  const [toast, setToast] = useState<ToastState | null>(null)

  const refreshQueue = useCallback(async (showLoading = false) => {
    if (showLoading) {
      setIsLoading(true)
    }

    setErrorMessage(null)

    try {
      const queue = await loadEscalationQueue()
      setItems(queue)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Failed to load the escalation queue.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void refreshQueue(true)

    const intervalId = window.setInterval(() => {
      void refreshQueue(false)
    }, REFRESH_INTERVAL_MS)

    return () => window.clearInterval(intervalId)
  }, [refreshQueue])

  useEffect(() => {
    if (!toast) {
      return undefined
    }

    const timeoutId = window.setTimeout(() => setToast(null), 4000)
    return () => window.clearTimeout(timeoutId)
  }, [toast])

  async function handleDecision(trackingId: string, decision: ApprovalDecision) {
    setDecidingTrackingId(trackingId)

    try {
      const response = await submitApprovalDecision(trackingId, {
        decision,
        comment: `${decision}d from escalation queue.`,
      })

      setToast({
        message: `Expense ${decision.toLowerCase()}d. Status: ${response.status}.`,
        variant: 'success',
      })

      await refreshQueue(false)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? (error.details?.join(' ') ?? error.message)
          : `Failed to ${decision.toLowerCase()} expense.`

      setToast({
        message,
        variant: 'error',
      })
    } finally {
      setDecidingTrackingId(null)
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
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
              Approvals Queue
            </h2>
            <p className="mt-1 text-sm text-slate-600">
              {canDecide
                ? 'Review escalated submissions and approve or reject each expense.'
                : 'View escalated items awaiting approver action.'}
            </p>
          </div>
          <button
            type="button"
            onClick={() => void refreshQueue(true)}
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 shadow-sm transition-colors hover:bg-slate-50"
          >
            Refresh
          </button>
        </div>

        <EscalationQueueTable
          items={items}
          isLoading={isLoading}
          errorMessage={errorMessage}
          onRetry={() => void refreshQueue(true)}
          canDecide={canDecide}
          decidingTrackingId={decidingTrackingId}
          onDecision={(trackingId, decision) => void handleDecision(trackingId, decision)}
        />
      </div>
    </>
  )
}
