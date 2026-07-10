import { formatBackendStatusLabel } from '../../utils/workflowSteps'

const STATUS_STYLES: Record<string, string> = {
  Received: 'bg-slate-100 text-slate-800 ring-slate-200',
  Processing: 'bg-blue-100 text-blue-800 ring-blue-200',
  AutoApproved: 'bg-emerald-100 text-emerald-800 ring-emerald-200',
  Escalated: 'bg-violet-100 text-violet-800 ring-violet-200',
  HumanApproved: 'bg-teal-100 text-teal-800 ring-teal-200',
  Rejected: 'bg-red-100 text-red-800 ring-red-200',
  PaymentPending: 'bg-amber-100 text-amber-800 ring-amber-200',
  Paid: 'bg-emerald-100 text-emerald-800 ring-emerald-200',
  PaymentFailed: 'bg-rose-100 text-rose-800 ring-rose-200',
  Compensated: 'bg-amber-100 text-amber-900 ring-amber-200',
}

type BackendStatusBadgeProps = {
  status: string
}

export function BackendStatusBadge({ status }: BackendStatusBadgeProps) {
  const style = STATUS_STYLES[status] ?? 'bg-slate-100 text-slate-800 ring-slate-200'

  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${style}`}
    >
      {formatBackendStatusLabel(status)}
    </span>
  )
}
