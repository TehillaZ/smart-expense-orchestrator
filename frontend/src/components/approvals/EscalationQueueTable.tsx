import { Link } from 'react-router-dom'
import { MobileDataCard, MobileDataList, MobileDataRow } from '../ui/MobileDataCard'
import type { ApprovalDecision, EscalationQueueItem } from '../../types/api'
import {
  getInfoRequestMessage,
  isInfoRequestedQueueItem,
} from '../../utils/requestInfo'

type EscalationQueueTableProps = {
  items: EscalationQueueItem[]
  isLoading?: boolean
  errorMessage?: string | null
  onRetry?: () => void
  canDecide?: boolean
  decidingTrackingId?: string | null
  highlightedTrackingIds?: ReadonlySet<string>
  onRequestDecision?: (trackingId: string, decision: ApprovalDecision) => void
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
  'rounded-lg px-3 py-2 text-xs font-semibold text-white shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-1 disabled:cursor-not-allowed disabled:opacity-60'

const ACTION_BUTTON_VARIANTS = {
  approve: 'bg-emerald-600 hover:bg-emerald-700 focus:ring-emerald-300',
  reject: 'bg-red-600 hover:bg-red-700 focus:ring-red-300',
  requestInfo: 'bg-amber-600 hover:bg-amber-700 focus:ring-amber-300',
} as const

type EscalationQueueActionsProps = {
  item: EscalationQueueItem
  canDecide: boolean
  isDeciding: boolean
  onRequestDecision?: (trackingId: string, decision: ApprovalDecision) => void
  layout?: 'stack' | 'row'
}

function EscalationQueueActions({
  item,
  canDecide,
  isDeciding,
  onRequestDecision,
  layout = 'stack',
}: EscalationQueueActionsProps) {
  const containerClass =
    layout === 'row' ? 'flex flex-wrap gap-2' : 'flex flex-col gap-2 sm:flex-row sm:flex-wrap'

  return (
    <div className={containerClass}>
      <Link
        to={`/approvals/${encodeURIComponent(item.trackingId)}`}
        className="inline-flex items-center justify-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs font-semibold text-blue-700 hover:bg-slate-50"
      >
        Review details
      </Link>
      {canDecide ? (
        <>
          <button
            type="button"
            disabled={isDeciding}
            onClick={() => onRequestDecision?.(item.trackingId, 'Approve')}
            className={`${ACTION_BUTTON_BASE} ${ACTION_BUTTON_VARIANTS.approve}`}
          >
            Approve
          </button>
          <button
            type="button"
            disabled={isDeciding}
            onClick={() => onRequestDecision?.(item.trackingId, 'Reject')}
            className={`${ACTION_BUTTON_BASE} ${ACTION_BUTTON_VARIANTS.reject}`}
          >
            Reject
          </button>
          <button
            type="button"
            disabled={isDeciding}
            onClick={() => onRequestDecision?.(item.trackingId, 'RequestInfo')}
            className={`${ACTION_BUTTON_BASE} ${ACTION_BUTTON_VARIANTS.requestInfo}`}
          >
            Request Info
          </button>
        </>
      ) : null}
    </div>
  )
}

type EscalationQueueItemCardProps = {
  item: EscalationQueueItem
  canDecide: boolean
  isDeciding: boolean
  isHighlighted: boolean
  onRequestDecision?: (trackingId: string, decision: ApprovalDecision) => void
}

function EscalationQueueItemCard({
  item,
  canDecide,
  isDeciding,
  isHighlighted,
  onRequestDecision,
}: EscalationQueueItemCardProps) {
  const infoRequested = isInfoRequestedQueueItem(item)
  const infoMessage = getInfoRequestMessage(item)

  return (
    <MobileDataCard
      className={[
        isHighlighted ? 'border-amber-300 bg-amber-50' : '',
        infoRequested && !isHighlighted ? 'border-amber-200 bg-amber-50/50' : '',
      ].join(' ')}
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-mono text-sm font-semibold text-slate-900">{item.invoiceNumber}</p>
          <p className="mt-0.5 break-all font-mono text-xs text-slate-500">{item.trackingId}</p>
        </div>
        <p className="text-sm font-semibold text-slate-900">{formatAmount(item.amount)}</p>
      </div>

      <MobileDataList className="mt-4">
        <MobileDataRow label="Vendor" value={item.vendor} />
        <MobileDataRow label="Category" value={item.category} />
        <MobileDataRow
          label="AI"
          value={
            <div>
              <span className="inline-flex rounded-full bg-violet-100 px-2.5 py-1 text-xs font-semibold text-violet-800 ring-1 ring-inset ring-violet-200">
                {item.aiRecommendation}
              </span>
              <p className="mt-1 text-xs text-slate-500">
                Confidence: {formatConfidence(item.confidence)}
              </p>
            </div>
          }
        />
        <MobileDataRow
          label="Rules"
          value={item.citedRules.length > 0 ? item.citedRules.join(', ') : '—'}
        />
        <MobileDataRow
          label="Reason"
          value={
            <div>
              {infoRequested ? (
                <span className="mb-2 inline-flex rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-900 ring-1 ring-inset ring-amber-200">
                  Info requested
                </span>
              ) : null}
              <p>{item.reason}</p>
              {infoRequested && item.humanDecisionComment ? (
                <p className="mt-2 text-xs text-amber-900">
                  <span className="font-medium">Question:</span> {infoMessage}
                </p>
              ) : null}
            </div>
          }
        />
      </MobileDataList>

      <div className="mt-4 border-t border-slate-200 pt-4">
        <EscalationQueueActions
          item={item}
          canDecide={canDecide}
          isDeciding={isDeciding}
          onRequestDecision={onRequestDecision}
          layout="row"
        />
      </div>
    </MobileDataCard>
  )
}

export function EscalationQueueTable({
  items,
  isLoading = false,
  errorMessage = null,
  onRetry,
  canDecide = false,
  decidingTrackingId = null,
  highlightedTrackingIds = new Set<string>(),
  onRequestDecision,
}: EscalationQueueTableProps) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-200 px-4 py-4 sm:px-6 sm:py-5">
        <h2 className="text-lg font-semibold text-slate-900">Escalation Queue</h2>
        <p className="mt-1 text-sm text-slate-500">
          Only items the system escalated appear here. Review AI guidance, cited rules, and
          escalation reason before deciding.
        </p>
      </div>

      {errorMessage ? (
        <div className="mx-4 mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 sm:mx-6">
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

      {isLoading ? (
        <p className="px-4 py-10 text-center text-sm text-slate-500 sm:px-6">
          Loading escalation queue…
        </p>
      ) : items.length === 0 ? (
        <p className="px-4 py-10 text-center text-sm text-slate-500 sm:px-6">
          No escalated items right now.
        </p>
      ) : (
        <>
          <div className="space-y-4 p-4 lg:hidden">
            {items.map((item) => (
              <EscalationQueueItemCard
                key={item.trackingId}
                item={item}
                canDecide={canDecide}
                isDeciding={decidingTrackingId === item.trackingId}
                isHighlighted={highlightedTrackingIds.has(item.trackingId)}
                onRequestDecision={onRequestDecision}
              />
            ))}
          </div>

          <div className="hidden overflow-x-auto lg:block">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                    Invoice
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                    Vendor
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                    Amount
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                    AI
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                    Rules cited
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                    Escalation reason
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                    Review
                  </th>
                  {canDecide ? (
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 xl:px-6">
                      Actions
                    </th>
                  ) : null}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {items.map((item) => {
                  const isDeciding = decidingTrackingId === item.trackingId
                  const infoRequested = isInfoRequestedQueueItem(item)
                  const isHighlighted = highlightedTrackingIds.has(item.trackingId)
                  const infoMessage = getInfoRequestMessage(item)

                  return (
                    <tr
                      key={item.trackingId}
                      className="align-top transition-colors hover:bg-slate-50"
                    >
                      <td className="px-4 py-4 xl:px-6">
                        <p className="font-mono text-sm font-medium text-slate-900">
                          {item.invoiceNumber}
                        </p>
                        <p className="mt-0.5 break-all font-mono text-xs text-slate-500">
                          {item.trackingId}
                        </p>
                        <p className="mt-1 text-xs text-slate-500">{item.category}</p>
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-700 xl:px-6">{item.vendor}</td>
                      <td className="whitespace-nowrap px-4 py-4 text-sm font-medium text-slate-900 xl:px-6">
                        {formatAmount(item.amount)}
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-700 xl:px-6">
                        <span className="inline-flex rounded-full bg-violet-100 px-2.5 py-1 text-xs font-semibold text-violet-800 ring-1 ring-inset ring-violet-200">
                          {item.aiRecommendation}
                        </span>
                        <p className="mt-1 text-xs text-slate-500">
                          Confidence: {formatConfidence(item.confidence)}
                        </p>
                      </td>
                      <td className="max-w-[10rem] px-4 py-4 text-xs text-slate-700 xl:px-6">
                        {item.citedRules.length > 0 ? item.citedRules.join(', ') : '—'}
                      </td>
                      <td
                        className={[
                          'max-w-xs px-4 py-4 text-sm text-slate-600 xl:px-6',
                          isHighlighted ? 'bg-amber-100 ring-2 ring-inset ring-amber-300' : '',
                          infoRequested && !isHighlighted ? 'bg-amber-50/80' : '',
                        ].join(' ')}
                      >
                        {infoRequested ? (
                          <div className="mb-2">
                            <span className="inline-flex rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-900 ring-1 ring-inset ring-amber-200">
                              Info requested
                            </span>
                          </div>
                        ) : null}
                        <p>{item.reason}</p>
                        {infoRequested && item.humanDecisionComment ? (
                          <p className="mt-2 text-xs text-amber-900">
                            <span className="font-medium">Question:</span> {infoMessage}
                          </p>
                        ) : null}
                      </td>
                      <td className="whitespace-nowrap px-4 py-4 text-sm xl:px-6">
                        <Link
                          to={`/approvals/${encodeURIComponent(item.trackingId)}`}
                          className="font-medium text-blue-700 underline hover:text-blue-900"
                        >
                          Review
                        </Link>
                      </td>
                      {canDecide ? (
                        <td className="whitespace-nowrap px-4 py-4 xl:px-6">
                          <EscalationQueueActions
                            item={item}
                            canDecide={canDecide}
                            isDeciding={isDeciding}
                            onRequestDecision={onRequestDecision}
                          />
                        </td>
                      ) : null}
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  )
}
