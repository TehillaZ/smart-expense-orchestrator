import type { SubmissionStatus } from '../../types/submission'

const STATUS_STYLES: Record<SubmissionStatus, string> = {
  Approved: 'bg-emerald-100 text-emerald-800 ring-emerald-200',
  Rejected: 'bg-red-100 text-red-800 ring-red-200',
  Pending: 'bg-amber-100 text-amber-800 ring-amber-200',
  Escalated: 'bg-violet-100 text-violet-800 ring-violet-200',
}

type StatusBadgeProps = {
  status: SubmissionStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  )
}
