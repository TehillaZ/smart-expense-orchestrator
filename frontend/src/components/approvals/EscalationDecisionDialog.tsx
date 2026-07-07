import { useState } from 'react'
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
  const isRequestInfo = decision === 'RequestInfo'
  const [comment, setComment] = useState('')
  const trimmedComment = comment.trim()
  const canSubmit = !isRequestInfo || trimmedComment.length > 0

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div
        className={[
          'w-full max-w-md rounded-xl border bg-white p-6 shadow-xl',
          isRequestInfo ? 'border-amber-300' : 'border-slate-200',
        ].join(' ')}
        role="dialog"
        aria-modal="true"
        aria-labelledby="decision-dialog-title"
      >
        <h3
          id="decision-dialog-title"
          className={[
            'text-lg font-semibold',
            isRequestInfo ? 'text-amber-950' : 'text-slate-900',
          ].join(' ')}
        >
          {DECISION_LABELS[decision]}
        </h3>
        <p className="mt-1 text-sm text-slate-600">
          {item.invoiceNumber} · {item.vendor} · ${item.amount.toFixed(2)}
        </p>
        {isRequestInfo ? (
          <p className="mt-2 text-sm text-amber-900">
            Tell the submitter what detail or receipt you need. They will see this on My
            Submissions.
          </p>
        ) : null}

        <form
          className="mt-4 space-y-4"
          onSubmit={(event) => {
            event.preventDefault()
            if (!canSubmit) {
              return
            }

            onConfirm(trimmedComment)
          }}
        >
          <div>
            <label htmlFor="approver-comment" className="mb-1.5 block text-sm font-medium text-slate-700">
              {isRequestInfo ? 'Question for submitter (required)' : 'Comment (optional)'}
            </label>
            <textarea
              id="approver-comment"
              name="comment"
              rows={3}
              value={comment}
              onChange={(event) => setComment(event.target.value)}
              placeholder={
                isRequestInfo
                  ? 'What receipt or detail do you need from the submitter?'
                  : 'Add context for the submitter or audit trail'
              }
              className={[
                'w-full rounded-lg border px-3 py-2 text-sm text-slate-900 shadow-sm focus:outline-none focus:ring-2',
                isRequestInfo
                  ? 'border-amber-300 focus:border-amber-500 focus:ring-amber-100'
                  : 'border-slate-300 focus:border-blue-500 focus:ring-blue-100',
              ].join(' ')}
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
              disabled={isSubmitting || !canSubmit}
              className={[
                'rounded-lg px-4 py-2 text-sm font-semibold text-white disabled:opacity-60',
                isRequestInfo
                  ? 'bg-amber-600 hover:bg-amber-700'
                  : 'bg-slate-900 hover:bg-slate-800',
              ].join(' ')}
            >
              {isSubmitting ? 'Saving…' : 'Confirm'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
