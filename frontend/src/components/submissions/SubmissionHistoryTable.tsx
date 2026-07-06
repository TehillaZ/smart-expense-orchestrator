import type { SubmissionRecord } from '../../types/submission'
import { BackendStatusBadge } from './BackendStatusBadge'
import { StatusBadge } from './StatusBadge'

type SubmissionHistoryTableProps = {
  submissions: SubmissionRecord[]
  isLoading?: boolean
  errorMessage?: string | null
  selectedTrackingId?: string | null
  onRetry?: () => void
  onSelect?: (trackingId: string) => void
}

function formatAmount(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

export function SubmissionHistoryTable({
  submissions,
  isLoading = false,
  errorMessage = null,
  selectedTrackingId = null,
  onRetry,
  onSelect,
}: SubmissionHistoryTableProps) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-200 px-6 py-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">Submission History</h2>
            <p className="mt-1 text-sm text-slate-500">
              Click a row to view the plain-language reason and workflow progress. Updates
              automatically every few seconds.
            </p>
          </div>
          {onRetry ? (
            <button
              type="button"
              onClick={onRetry}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 shadow-sm transition-colors hover:bg-slate-50"
            >
              Refresh
            </button>
          ) : null}
        </div>
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
                Tracking ID
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
                Status
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"
              >
                Reason
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {isLoading ? (
              <tr>
                <td colSpan={5} className="px-6 py-10 text-center text-sm text-slate-500">
                  Loading submission history…
                </td>
              </tr>
            ) : submissions.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-6 py-10 text-center text-sm text-slate-500">
                  No submissions yet. Submit your first expense to get started.
                </td>
              </tr>
            ) : (
              submissions.map((submission) => {
                const isSelected = selectedTrackingId === submission.trackingId

                return (
                  <tr
                    key={submission.trackingId}
                    className={[
                      'cursor-pointer transition-colors hover:bg-slate-50',
                      isSelected ? 'bg-blue-50/70' : '',
                    ].join(' ')}
                    onClick={() => onSelect?.(submission.trackingId)}
                  >
                    <td className="whitespace-nowrap px-6 py-4">
                      <p className="font-mono text-sm font-medium text-slate-900">
                        {submission.trackingId}
                      </p>
                      {submission.isDuplicateResubmit ? (
                        <span className="mt-1 inline-flex rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-blue-700 ring-1 ring-inset ring-blue-700/10">
                        Already Submitted
                        </span>
                      ) : null}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                      {submission.vendor}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-slate-900">
                      {formatAmount(submission.amount)}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="flex flex-col gap-1">
                        {submission.backendStatus ? (
                          <BackendStatusBadge status={submission.backendStatus} />
                        ) : (
                          <StatusBadge status={submission.status} />
                        )}
                      </div>
                    </td>
                    <td className="max-w-xs px-6 py-4 text-sm text-slate-600">
                      {submission.reason ?? '—'}
                    </td>
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
