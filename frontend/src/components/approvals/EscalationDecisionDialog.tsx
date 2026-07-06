import type { ApprovalDecision, EscalationQueueItem } from '../../types/api'

type EscalationDecisionDialogProps = {
  item: EscalationQueueItem
  decision: ApprovalDecision
  isSubmitting: boolean
  onConfirm: (comment: string) => void
  onCancel: () => void
}

const DECISION_LABELS: Record<ApprovalDecision, string> = {
  Approve: 'Approve expense',
  Reject: 'Reject expense',
  RequestInfo: 'Request more information',
}

export function EscalationDecisionDialog({
  item,
  decision,
  isSubmitting,
  onConfirm,
  onCancel,
}: EscalationDecisionDialogProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div
        className="w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-xl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="decision-dialog-title"
      >
        <h3 id="decision-dialog-title" className="text-lg font-semibold text-slate-900">
          {DECISION_LABELS[decision]}
        </h3>
        <p className="mt-1 text-sm text-slate-600">
          {item.invoiceNumber} · {item.vendor} · ${item.amount.toFixed(2)}
        </p>

        <form
          className="mt-4 space-y-4"
          onSubmit={(event) => {
            event.preventDefault()
            const formData = new FormData(event.currentTarget)
            const comment = String(formData.get('comment') ?? '').trim()
            onConfirm(comment)
          }}
        >
          <div>
            <label htmlFor="approver-comment" className="mb-1.5 block text-sm font-medium text-slate-700">
              Comment (optional)
            </label>
            <textarea
              id="approver-comment"
              name="comment"
              rows={3}
              placeholder="Add context for the submitter or audit trail"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            />
          </div>

          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              onClick={onCancel}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-60"
            >
              {isSubmitting ? 'Saving…' : 'Confirm'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
