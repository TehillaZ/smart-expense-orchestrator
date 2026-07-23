import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { EscalationDecisionDialog } from '../components/approvals/EscalationDecisionDialog'
import { Toast } from '../components/ui/Toast'
import { useAppRole } from '../hooks/useAppRole'
import { ApiRequestError } from '../services/apiClient'
import {
  loadEscalationItem,
  submitApprovalDecision,
} from '../services/approvalService'
import type { ApprovalDecision, EscalationQueueItem } from '../types/api'
import { canDecideEscalations } from '../types/userRole'

type ToastState = {
  message: string
  variant: 'success' | 'error'
}

type PendingDecision = {
  decision: ApprovalDecision
}

function formatAmount(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

function formatConfidence(confidence: number): string {
  return `${Math.round(confidence * 100)}%`
}

export function EscalationDetailPage() {
  const { trackingId = '' } = useParams()
  const navigate = useNavigate()
  const role = useAppRole()
  const canDecide = canDecideEscalations(role)
  const [item, setItem] = useState<EscalationQueueItem | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [pendingDecision, setPendingDecision] = useState<PendingDecision | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [toast, setToast] = useState<ToastState | null>(null)

  const refreshItem = useCallback(async () => {
    if (!trackingId) {
      return
    }

    setIsLoading(true)
    setErrorMessage(null)

    try {
      const escalationItem = await loadEscalationItem(trackingId)
      if (!escalationItem) {
        setItem(null)
        setErrorMessage('Escalated submission was not found.')
        return
      }

      setItem(escalationItem)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Failed to load escalation details.'
      setErrorMessage(message)
      setItem(null)
    } finally {
      setIsLoading(false)
    }
  }, [trackingId])

  useEffect(() => {
    void refreshItem()
  }, [refreshItem])

  useEffect(() => {
    if (!toast) {
      return undefined
    }

    const timeoutId = window.setTimeout(() => setToast(null), 4000)
    return () => window.clearTimeout(timeoutId)
  }, [toast])

  async function handleConfirmDecision(comment: string) {
    if (!pendingDecision || !item) {
      return
    }

    setIsSubmitting(true)

    try {
      const response = await submitApprovalDecision(item.trackingId, {
        decision: pendingDecision.decision,
        comment: comment || undefined,
      })

      const decisionLabel =
        pendingDecision.decision === 'RequestInfo'
          ? 'sent back for more info'
          : `${pendingDecision.decision.toLowerCase()}d`

      setToast({
        message: `Expense ${decisionLabel}. Status: ${response.status}.`,
        variant: 'success',
      })

      setPendingDecision(null)
      navigate('/approvals', { replace: true })
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? (error.details?.join(' ') ?? error.message)
          : 'Failed to apply the approval decision.'

      setToast({ message, variant: 'error' })
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return <p className="text-sm text-slate-500">Loading escalation details…</p>
  }

  if (errorMessage || !item) {
    return (
      <div className="space-y-4">
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {errorMessage ?? 'Escalated submission was not found.'}
        </div>
        <Link to="/approvals" className="text-sm font-medium text-blue-700 underline">
          Back to escalation queue
        </Link>
      </div>
    )
  }

  return (
    <>
      {toast ? (
        <Toast message={toast.message} variant={toast.variant} onDismiss={() => setToast(null)} />
      ) : null}

      {pendingDecision ? (
        <EscalationDecisionDialog
          item={item}
          decision={pendingDecision.decision}
          isSubmitting={isSubmitting}
          onConfirm={(comment) => void handleConfirmDecision(comment)}
          onCancel={() => setPendingDecision(null)}
        />
      ) : null}

      <div className="space-y-4 sm:space-y-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-start sm:justify-between">
          <div className="min-w-0">
            <Link to="/approvals" className="text-sm font-medium text-blue-700 underline">
              Back to queue
            </Link>
            <h2 className="mt-2 text-xl font-semibold tracking-tight text-slate-900 sm:text-2xl">
              Review Escalation
            </h2>
            <p className="mt-1 break-all font-mono text-xs text-slate-600 sm:text-sm">
              {item.trackingId}
            </p>
          </div>
        </div>

        <div className="flex flex-col gap-4 lg:grid lg:grid-cols-2 lg:gap-6">
          <section className="order-1 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:p-6">
            <h3 className="text-base font-semibold text-slate-900 sm:text-lg">Submission details</h3>
            <dl className="mt-4 space-y-4 text-sm sm:space-y-3">
              <div className="border-b border-slate-100 pb-3 last:border-b-0 last:pb-0 sm:border-b-0 sm:pb-0">
                <dt className="font-medium text-slate-500">Vendor</dt>
                <dd className="mt-0.5 text-slate-900">{item.vendor}</dd>
              </div>
              <div className="border-b border-slate-100 pb-3 last:border-b-0 last:pb-0 sm:border-b-0 sm:pb-0">
                <dt className="font-medium text-slate-500">Invoice</dt>
                <dd className="mt-0.5 break-all font-mono text-slate-900">{item.invoiceNumber}</dd>
              </div>
              <div className="border-b border-slate-100 pb-3 last:border-b-0 last:pb-0 sm:border-b-0 sm:pb-0">
                <dt className="font-medium text-slate-500">Amount</dt>
                <dd className="mt-0.5 text-slate-900">{formatAmount(item.amount)}</dd>
              </div>
              <div className="border-b border-slate-100 pb-3 last:border-b-0 last:pb-0 sm:border-b-0 sm:pb-0">
                <dt className="font-medium text-slate-500">Category</dt>
                <dd className="mt-0.5 text-slate-900">{item.category}</dd>
              </div>
              <div>
                <dt className="font-medium text-slate-500">Escalation reason</dt>
                <dd className="mt-0.5 text-slate-900">{item.reason}</dd>
              </div>
            </dl>
          </section>

          <section className="order-2 rounded-xl border border-violet-200 bg-violet-50 p-4 shadow-sm sm:p-6">
            <h3 className="text-base font-semibold text-violet-950 sm:text-lg">AI recommendation</h3>
            <p className="mt-3 inline-flex rounded-full bg-violet-100 px-3 py-1 text-sm font-semibold text-violet-900 ring-1 ring-inset ring-violet-200">
              {item.aiRecommendation}
            </p>
            <p className="mt-3 text-sm text-violet-900">
              Confidence: {formatConfidence(item.confidence)}
            </p>
            {item.citedRules.length > 0 ? (
              <p className="mt-3 break-words text-sm text-violet-900">
                <span className="font-medium">Cited rules:</span> {item.citedRules.join(', ')}
              </p>
            ) : null}
            {item.aiReasoningSummary ? (
              <p className="mt-3 break-words text-sm text-violet-900">
                <span className="font-medium">AI reasoning:</span> {item.aiReasoningSummary}
              </p>
            ) : null}
          </section>
        </div>

        {canDecide ? (
          <section className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:p-6">
            <h3 className="text-base font-semibold text-slate-900 sm:text-lg">Approver decision</h3>
            <p className="mt-1 text-sm text-slate-600">
              Choose an action to resume the paused workflow.
            </p>
            <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
              <button
                type="button"
                onClick={() => setPendingDecision({ decision: 'Approve' })}
                className="w-full rounded-lg bg-emerald-600 px-4 py-3 text-sm font-semibold text-white hover:bg-emerald-700 sm:w-auto sm:py-2"
              >
                Approve
              </button>
              <button
                type="button"
                onClick={() => setPendingDecision({ decision: 'Reject' })}
                className="w-full rounded-lg bg-red-600 px-4 py-3 text-sm font-semibold text-white hover:bg-red-700 sm:w-auto sm:py-2"
              >
                Reject
              </button>
              <button
                type="button"
                onClick={() => setPendingDecision({ decision: 'RequestInfo' })}
                className="w-full rounded-lg bg-amber-600 px-4 py-3 text-sm font-semibold text-white hover:bg-amber-700 sm:w-auto sm:py-2"
              >
                Request More Info
              </button>
            </div>
          </section>
        ) : null}
      </div>
    </>
  )
}
