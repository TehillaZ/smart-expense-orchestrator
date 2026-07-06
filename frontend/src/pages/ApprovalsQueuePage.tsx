import { useCallback, useEffect, useState } from 'react'
import { EscalationDecisionDialog } from '../components/approvals/EscalationDecisionDialog'
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

type PendingDecision = {
  item: EscalationQueueItem
  decision: ApprovalDecision
}

export function ApprovalsQueuePage() {
  const role = useAppRole()
  const canDecide = canDecideEscalations(role)
  const [items, setItems] = useState<EscalationQueueItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [decidingTrackingId, setDecidingTrackingId] = useState<string | null>(null)
  const [pendingDecision, setPendingDecision] = useState<PendingDecision | null>(null)
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

  function handleRequestDecision(trackingId: string, decision: ApprovalDecision) {
    const item = items.find((entry) => entry.trackingId === trackingId)
    if (!item) {
      return
    }

    setPendingDecision({ item, decision })
  }

  async function handleConfirmDecision(comment: string) {
    if (!pendingDecision) {
      return
    }

    const { item, decision } = pendingDecision
    setDecidingTrackingId(item.trackingId)

    try {
      const response = await submitApprovalDecision(item.trackingId, {
        decision,
        comment: comment || undefined,
      })

      const decisionLabel =
        decision === 'RequestInfo'
          ? 'sent back for more info'
          : `${decision.toLowerCase()}d`

      setToast({
        message: `Expense ${decisionLabel}. Status: ${response.status}.`,
        variant: 'success',
      })

      setPendingDecision(null)
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

      {pendingDecision ? (
        <EscalationDecisionDialog
          item={pendingDecision.item}
          decision={pendingDecision.decision}
          isSubmitting={decidingTrackingId === pendingDecision.item.trackingId}
          onConfirm={(comment) => void handleConfirmDecision(comment)}
          onCancel={() => setPendingDecision(null)}
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
                ? 'Review escalated submissions with AI guidance, cited rules, and policy reasons.'
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
          onRequestDecision={handleRequestDecision}
        />
      </div>
    </>
  )
}
