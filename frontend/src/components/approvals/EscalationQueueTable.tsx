import type { ApprovalDecision, EscalationQueueItem } from '../../types/api'

type EscalationQueueTableProps = {
  items: EscalationQueueItem[]
  isLoading?: boolean
  errorMessage?: string | null
  onRetry?: () => void
  canDecide?: boolean
  decidingTrackingId?: string | null
  onDecision?: (trackingId: string, decision: ApprovalDecision) => void
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

const ACTION_BUTTON_BASE =
  'rounded-lg px-3 py-1.5 text-xs font-semibold text-white shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-1 disabled:cursor-not-allowed disabled:opacity-60'

const ACTION_BUTTON_VARIANTS = {
  approve: 'bg-emerald-600 hover:bg-emerald-700 focus:ring-emerald-300',
  reject: 'bg-red-600 hover:bg-red-700 focus:ring-red-300',
} as const

export function EscalationQueueTable({
  items,
  isLoading = false,
  errorMessage = null,
  onRetry,
  canDecide = false,
  decidingTrackingId = null,
  onDecision,
}: EscalationQueueTableProps) {
  const columnCount = canDecide ? 7 : 6

  return (
    <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-200 px-6 py-5">
        <h2 className="text-lg font-semibold text-slate-900">Escalation Queue</h2>
        <p className="mt-1 text-sm text-slate-500">
          Items awaiting a human approver decision. Submit an expense over $400 or use category
          &quot;Other&quot; to trigger escalation.
        </p>
      </div>

      {errorMessage ? (
        <div className="mx-6 mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          <p>{errorMessage}</p>
          {onRetry ? (
            <button
              type="button"
              onClick={onRetry}
              className="mt-2 text-xs font-semibold uppercase tracking-wide text-red-700 underline"
            >
              Try again
            </button>
          ) : null}
        </div>
      ) : null}

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200">
          <thead className="bg-slate-50">
            <tr>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
              >
                Invoice
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
              >
                Vendor
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
              >
                Amount
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
              >
                Category
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
              >
                AI Recommendation
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
              >
                Confidence
              </th>
              {canDecide ? (
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
                >
                  Actions
                </th>
              ) : null}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {isLoading ? (
              <tr>
                <td colSpan={columnCount} className="px-6 py-10 text-center text-sm text-slate-500">
                  Loading escalation queue…
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={columnCount} className="px-6 py-10 text-center text-sm text-slate-500">
                  No escalated items right now.
                </td>
              </tr>
            ) : (
              items.map((item) => {
                const isDeciding = decidingTrackingId === item.trackingId

                return (
                  <tr key={item.trackingId} className="transition-colors hover:bg-slate-50">
                    <td className="whitespace-nowrap px-6 py-4">
                      <p className="font-mono text-sm font-medium text-slate-900">
                        {item.invoiceNumber}
                      </p>
                      <p className="mt-0.5 font-mono text-xs text-slate-500">{item.trackingId}</p>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                      {item.vendor}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-slate-900">
                      {formatAmount(item.amount)}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                      {item.category}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <span className="inline-flex items-center rounded-full bg-violet-100 px-2.5 py-1 text-xs font-semibold text-violet-800 ring-1 ring-inset ring-violet-200">
                        {item.aiRecommendation}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                      {formatConfidence(item.confidence)}
                    </td>
                    {canDecide ? (
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="flex flex-wrap gap-2">
                          <button
                            type="button"
                            disabled={isDeciding}
                            onClick={() => onDecision?.(item.trackingId, 'Approve')}
                            className={`${ACTION_BUTTON_BASE} ${ACTION_BUTTON_VARIANTS.approve}`}
                          >
                            {isDeciding ? 'Saving…' : 'Approve'}
                          </button>
                          <button
                            type="button"
                            disabled={isDeciding}
                            onClick={() => onDecision?.(item.trackingId, 'Reject')}
                            className={`${ACTION_BUTTON_BASE} ${ACTION_BUTTON_VARIANTS.reject}`}
                          >
                            Reject
                          </button>
                        </div>
                      </td>
                    ) : null}
                  </tr>
                )
              })
            )}
          </tbody>
        </table>
      </div>
    </section>
  )
}
