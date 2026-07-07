import { Link } from 'react-router-dom'
import type { SubmissionDetail } from '../../types/submission'
import { useAppRole } from '../../hooks/useAppRole'
import { canViewAuditTrail } from '../../types/userRole'
import { getInfoRequestMessage, isInfoRequestedSubmission } from '../../utils/requestInfo'
import { BackendStatusBadge } from './BackendStatusBadge'
import { WorkflowStepper } from './WorkflowStepper'

type SubmissionDetailPanelProps = {
  detail: SubmissionDetail
  onClose: () => void
  isDuplicateResubmit?: boolean
}

function formatAmount(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

export function SubmissionDetailPanel({
  detail,
  onClose,
  isDuplicateResubmit = false,
}: SubmissionDetailPanelProps) {
  const role = useAppRole()
  const showAuditLink = canViewAuditTrail(role)
  const backendStatus = detail.backendStatus ?? detail.status
  const infoRequested = isInfoRequestedSubmission(detail)
  const infoRequestMessage = getInfoRequestMessage(detail)

  return (
    <section className="rounded-xl border border-blue-200 bg-blue-50/40 p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">Submission Status</h3>
          <p className="mt-1 font-mono text-sm text-slate-600">{detail.trackingId}</p>
          <p className="mt-1 font-mono text-xs text-slate-500">
            Correlation: {detail.correlationId}
          </p>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
        >
          Close
        </button>
      </div>

      {isDuplicateResubmit ? (
        <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          This tracking id was returned from a duplicate resubmit
          {detail.duplicateResubmitCount && detail.duplicateResubmitCount > 1
            ? ` (${detail.duplicateResubmitCount} times)`
            : ''}
          . The original submission is preserved and payment will not run again.
        </div>
      ) : null}

      {infoRequested ? (
        <div className="mt-4 rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-950">
          <p className="font-semibold">Action required from you</p>
          <p className="mt-1">
            Your approver requested more information before deciding on this expense.
          </p>
          <p className="mt-2 rounded-md border border-amber-200 bg-white px-3 py-2 text-slate-800">
            {infoRequestMessage}
          </p>
          <p className="mt-2 text-xs text-amber-900">
            Provide the requested detail to your approver outside this demo app. The submission
            stays escalated until they approve or reject.
          </p>
        </div>
      ) : null}

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <BackendStatusBadge status={backendStatus} />
        {detail.aiRecommendation ? (
          <span className="text-xs text-slate-600">
            AI: {detail.aiRecommendation}
            {detail.aiConfidence != null
              ? ` (${Math.round(detail.aiConfidence * 100)}%)`
              : ''}
          </span>
        ) : null}
      </div>

      <p className="mt-4 rounded-lg border border-slate-200 bg-white px-4 py-3 text-sm text-slate-800">
        <span className="font-medium text-slate-900">Outcome reason: </span>
        {detail.reason}
      </p>

      {detail.notificationReason && detail.notificationReason !== detail.reason ? (
        <p className="mt-2 text-sm text-slate-600">
          <span className="font-medium">Payment note: </span>
          {detail.notificationReason}
        </p>
      ) : null}

      <div className="mt-6">
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
          Workflow progress
        </p>
        <WorkflowStepper backendStatus={backendStatus} />
      </div>

      <dl className="mt-6 grid grid-cols-1 gap-4 text-sm sm:grid-cols-2">
        <div>
          <dt className="font-medium text-slate-500">Vendor</dt>
          <dd className="text-slate-900">{detail.vendor}</dd>
        </div>
        <div>
          <dt className="font-medium text-slate-500">Invoice</dt>
          <dd className="font-mono text-slate-900">{detail.invoiceNumber}</dd>
        </div>
        <div>
          <dt className="font-medium text-slate-500">Amount</dt>
          <dd className="text-slate-900">{formatAmount(detail.amount)}</dd>
        </div>
        <div>
          <dt className="font-medium text-slate-500">Category</dt>
          <dd className="text-slate-900">{detail.category}</dd>
        </div>
        {detail.citedRules && detail.citedRules.length > 0 ? (
          <div className="sm:col-span-2">
            <dt className="font-medium text-slate-500">Policy rules applied</dt>
            <dd className="text-slate-900">{detail.citedRules.join(', ')}</dd>
          </div>
        ) : null}
        {detail.autonomyCeilingAtDecision != null ? (
          <div>
            <dt className="font-medium text-slate-500">Ceiling at decision</dt>
            <dd className="text-slate-900">{formatAmount(detail.autonomyCeilingAtDecision)}</dd>
          </div>
        ) : null}
      </dl>

      {showAuditLink ? (
        <div className="mt-4 flex flex-wrap gap-3">
          <Link
            to={`/audit?trackingId=${encodeURIComponent(detail.trackingId)}`}
            className="text-sm font-medium text-blue-700 underline hover:text-blue-900"
          >
            View full audit trail
          </Link>
        </div>
      ) : null}
    </section>
  )
}
