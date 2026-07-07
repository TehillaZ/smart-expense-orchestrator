import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { EscalationDecisionDialog } from '../components/approvals/EscalationDecisionDialog'
import { EscalationQueueFiltersBar } from '../components/approvals/EscalationQueueFiltersBar'
import { EscalationQueueTable } from '../components/approvals/EscalationQueueTable'
import { Toast } from '../components/ui/Toast'
import { useAppRole } from '../hooks/useAppRole'
import { ApiRequestError } from '../services/apiClient'
import { loadEscalationQueue, submitApprovalDecision } from '../services/approvalService'
import type { ApprovalDecision, EscalationQueueItem } from '../types/api'
import { canDecideEscalations } from '../types/userRole'
import {
  EMPTY_ESCALATION_QUEUE_FILTERS,
  filterEscalationQueue,
  type EscalationQueueFilters,
} from '../utils/escalationQueueFilters'

const REFRESH_INTERVAL_MS = 5000
const HIGHLIGHT_DURATION_MS = 4000

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
  const [filters, setFilters] = useState<EscalationQueueFilters>(EMPTY_ESCALATION_QUEUE_FILTERS)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [decidingTrackingId, setDecidingTrackingId] = useState<string | null>(null)
  const [pendingDecision, setPendingDecision] = useState<PendingDecision | null>(null)
  const [toast, setToast] = useState<ToastState | null>(null)
  const [highlightedTrackingIds, setHighlightedTrackingIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  )
  const highlightTimeoutsRef = useRef<Map<string, number>>(new Map())
  const filteredItems = useMemo(() => filterEscalationQueue(items, filters), [items, filters])

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

  const markRecentlyUpdated = useCallback((trackingId: string) => {
    setHighlightedTrackingIds((current) => new Set([...current, trackingId]))

    const existingTimeout = highlightTimeoutsRef.current.get(trackingId)
    if (existingTimeout) {
      window.clearTimeout(existingTimeout)
    }

    const timeoutId = window.setTimeout(() => {
      setHighlightedTrackingIds((current) => {
        const next = new Set(current)
        next.delete(trackingId)
        return next
      })
      highlightTimeoutsRef.current.delete(trackingId)
    }, HIGHLIGHT_DURATION_MS)

    highlightTimeoutsRef.current.set(trackingId, timeoutId)
  }, [])

  useEffect(() => {
    void refreshQueue(true)

    const intervalId = window.setInterval(() => {
      void refreshQueue(false)
    }, REFRESH_INTERVAL_MS)

    const highlightTimeouts = highlightTimeoutsRef.current

    return () => {
      window.clearInterval(intervalId)
      for (const timeoutId of highlightTimeouts.values()) {
        window.clearTimeout(timeoutId)
      }
      highlightTimeouts.clear()
    }
  }, [refreshQueue])

  useEffect(() => {
    if (!toast) {
      return undefined
    }

    const timeoutId = window.setTimeout(() => setToast(null), 6000)
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

      if (decision === 'RequestInfo') {
        markRecentlyUpdated(item.trackingId)
        const preview =
          comment.length > 80 ? `${comment.slice(0, 80).trim()}…` : comment.trim()

        setToast({
          message: `Info requested. Escalation reason updated. Submitter will see: "${preview}" Recorded in audit trail.`,
          variant: 'success',
        })
      } else {
        const decisionLabel = `${decision.toLowerCase()}d`
        setToast({
          message: `Expense ${decisionLabel}. Status: ${response.status}.`,
          variant: 'success',
        })
      }

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

        <EscalationQueueFiltersBar
          filters={filters}
          onChange={setFilters}
          onReset={() => setFilters(EMPTY_ESCALATION_QUEUE_FILTERS)}
          resultCount={filteredItems.length}
          totalCount={items.length}
        />

        <EscalationQueueTable
          items={filteredItems}
          isLoading={isLoading}
          errorMessage={errorMessage}
          onRetry={() => void refreshQueue(true)}
          canDecide={canDecide}
          decidingTrackingId={decidingTrackingId}
          highlightedTrackingIds={highlightedTrackingIds}
          onRequestDecision={handleRequestDecision}
        />
      </div>
    </>
  )
}
