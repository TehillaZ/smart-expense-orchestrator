type SubmitSuccessPanelProps = {
  trackingId: string
  isDuplicate: boolean
  onViewStatus: () => void
  onDismiss: () => void
}

export function SubmitSuccessPanel({
  trackingId,
  isDuplicate,
  onViewStatus,
  onDismiss,
}: SubmitSuccessPanelProps) {
  return (
    <section className="rounded-xl border border-emerald-200 bg-emerald-50 p-6 shadow-sm">
      <p className="text-sm font-semibold uppercase tracking-wide text-emerald-800">
        {isDuplicate ? 'Existing submission returned' : 'Submission accepted'}
      </p>
      <p className="mt-2 font-mono text-2xl font-bold tracking-tight text-emerald-950">
        {trackingId}
      </p>
      <p className="mt-2 text-sm text-emerald-900">
        {isDuplicate
          ? 'This invoice was already submitted. No duplicate payment will be processed.'
          : 'Processing continues in the background. You can check status at any time.'}
      </p>
      <div className="mt-4 flex flex-wrap gap-3">
        <button
          type="button"
          onClick={onViewStatus}
          className="rounded-lg bg-emerald-700 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-800"
        >
          View Status
        </button>
        <button
          type="button"
          onClick={onDismiss}
          className="rounded-lg border border-emerald-300 bg-white px-4 py-2 text-sm font-medium text-emerald-900 hover:bg-emerald-100"
        >
          Dismiss
        </button>
      </div>
    </section>
  )
}
