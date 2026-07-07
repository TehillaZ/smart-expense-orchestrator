import type { SubmissionRecord } from '../../types/submission'
import { isInfoRequestedSubmission } from '../../utils/requestInfo'
import { MobileDataCard, MobileDataList, MobileDataRow } from '../ui/MobileDataCard'
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

function SubmissionStatusBadge({ submission }: { submission: SubmissionRecord }) {
  if (submission.backendStatus) {
    return <BackendStatusBadge status={submission.backendStatus} />
  }

  return <StatusBadge status={submission.status} />
}

type SubmissionHistoryCardProps = {
  submission: SubmissionRecord
  isSelected: boolean
  onSelect?: (trackingId: string) => void
}

function SubmissionHistoryCard({ submission, isSelected, onSelect }: SubmissionHistoryCardProps) {
  return (
    <MobileDataCard
      className={isSelected ? 'border-blue-300 bg-blue-50/70' : ''}
      onClick={() => onSelect?.(submission.trackingId)}
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="break-all font-mono text-sm font-semibold text-slate-900">
            {submission.trackingId}
          </p>
          {submission.isDuplicateResubmit ? (
            <span className="mt-1 inline-flex rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-blue-700 ring-1 ring-inset ring-blue-700/10">
              Already Submitted
              {(submission.duplicateResubmitCount ?? 0) > 1
                ? ` ×${submission.duplicateResubmitCount}`
                : ''}
            </span>
          ) : null}
        </div>
        <SubmissionStatusBadge submission={submission} />
      </div>

      <MobileDataList className="mt-4">
        <MobileDataRow label="Vendor" value={submission.vendor} />
        <MobileDataRow label="Amount" value={formatAmount(submission.amount)} />
        <MobileDataRow
          label="Reason"
          value={
            <div>
              {isInfoRequestedSubmission(submission) ? (
                <span className="mb-1 inline-flex rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-900 ring-1 ring-inset ring-amber-200">
                  Info requested
                </span>
              ) : null}
              <p>{submission.reason ?? '—'}</p>
            </div>
          }
        />
      </MobileDataList>
    </MobileDataCard>
  )
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
      <div className="border-b border-slate-200 px-4 py-4 sm:px-6 sm:py-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">Submission History</h2>
            <p className="mt-1 text-sm text-slate-500">
              Tap a row to view the plain-language reason and workflow progress. Updates
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
          Loading submission history…
        </p>
      ) : submissions.length === 0 ? (
        <p className="px-4 py-10 text-center text-sm text-slate-500 sm:px-6">
          No submissions yet. Submit your first expense to get started.
        </p>
      ) : (
        <>
          <div className="space-y-3 p-4 md:hidden">
            {submissions.map((submission) => (
              <SubmissionHistoryCard
                key={submission.trackingId}
                submission={submission}
                isSelected={selectedTrackingId === submission.trackingId}
                onSelect={onSelect}
              />
            ))}
          </div>

          <div className="hidden overflow-x-auto md:block">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th
                    scope="col"
                    className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6"
                  >
                    Tracking ID
                  </th>
                  <th
                    scope="col"
                    className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6"
                  >
                    Vendor
                  </th>
                  <th
                    scope="col"
                    className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6"
                  >
                    Amount
                  </th>
                  <th
                    scope="col"
                    className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6"
                  >
                    Status
                  </th>
                  <th
                    scope="col"
                    className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6"
                  >
                    Reason
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {submissions.map((submission) => {
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
                      <td className="px-4 py-4 sm:px-6">
                        <p className="break-all font-mono text-sm font-medium text-slate-900">
                          {submission.trackingId}
                        </p>
                        {submission.isDuplicateResubmit ? (
                          <span className="mt-1 inline-flex rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-blue-700 ring-1 ring-inset ring-blue-700/10">
                            Already Submitted
                            {(submission.duplicateResubmitCount ?? 0) > 1
                              ? ` ×${submission.duplicateResubmitCount}`
                              : ''}
                          </span>
                        ) : null}
                      </td>
                      <td className="px-4 py-4 text-sm text-slate-700 sm:px-6">
                        {submission.vendor}
                      </td>
                      <td className="whitespace-nowrap px-4 py-4 text-sm font-medium text-slate-900 sm:px-6">
                        {formatAmount(submission.amount)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-4 sm:px-6">
                        <SubmissionStatusBadge submission={submission} />
                      </td>
                      <td className="max-w-xs px-4 py-4 text-sm text-slate-600 sm:px-6">
                        {isInfoRequestedSubmission(submission) ? (
                          <span className="mb-1 inline-flex rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-900 ring-1 ring-inset ring-amber-200">
                            Info requested
                          </span>
                        ) : null}
                        <p>{submission.reason ?? '—'}</p>
                      </td>
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
