type ToastVariant = 'success' | 'error'

type ToastProps = {
  message: string
  variant: ToastVariant
  onDismiss: () => void
}

const VARIANT_STYLES: Record<ToastVariant, string> = {
  success: 'border-emerald-200 bg-emerald-50 text-emerald-900',
  error: 'border-red-200 bg-red-50 text-red-900',
}

export function Toast({ message, variant, onDismiss }: ToastProps) {
  return (
    <div className="pointer-events-none fixed inset-x-0 top-4 z-50 flex justify-center px-4">
      <div
        role="status"
        className={`pointer-events-auto flex max-w-md items-start gap-3 rounded-lg border px-4 py-3 text-sm shadow-lg ${VARIANT_STYLES[variant]}`}
      >
        <p className="flex-1">{message}</p>
        <button
          type="button"
          onClick={onDismiss}
          className="rounded-md px-1 text-xs font-semibold uppercase tracking-wide opacity-70 transition-opacity hover:opacity-100"
        >
          Close
        </button>
      </div>
    </div>
  )
}
