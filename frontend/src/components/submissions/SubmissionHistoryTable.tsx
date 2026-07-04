import type { SubmissionRecord } from '../../types/submission'
import { StatusBadge } from './StatusBadge'

type SubmissionHistoryTableProps = {
  submissions: SubmissionRecord[]
  isLoading?: boolean
  errorMessage?: string | null
  onRetry?: () => void
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
  onRetry,
}: SubmissionHistoryTableProps) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-200 px-6 py-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">Submission History</h2>
            <p className="mt-1 text-sm text-slate-500">
              Live statuses from the platform state store, enriched with notification outcomes.
              Updates automatically every few seconds.
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
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {isLoading ? (
              <tr>
                <td colSpan={4} className="px-6 py-10 text-center text-sm text-slate-500">
                  Loading submission history…
                </td>
              </tr>
            ) : submissions.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-6 py-10 text-center text-sm text-slate-500">
                  No submissions yet. Submit your first expense to get started.
                </td>
              </tr>
            ) : (
              submissions.map((submission) => (
                <tr key={submission.trackingId} className="transition-colors hover:bg-slate-50">
                  <td className="whitespace-nowrap px-6 py-4 font-mono text-sm font-medium text-slate-900">
                    {submission.trackingId}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                    {submission.vendor}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-slate-900">
                    {formatAmount(submission.amount)}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4">
                    <StatusBadge status={submission.status} />
                    {submission.backendStatus &&
                    submission.backendStatus !== submission.status ? (
                      <p className="mt-1 text-xs text-slate-500">{submission.backendStatus}</p>
                    ) : null}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  )
}
