import { FormEvent, useCallback, useEffect, useState } from 'react'
import { Toast } from '../components/ui/Toast'
import { useAppRole } from '../hooks/useAppRole'
import { ApiRequestError } from '../services/apiClient'
import { loadPolicySettings, updatePolicySettings } from '../services/policyService'
import type { PolicySettings } from '../types/api'

type ToastState = {
  message: string
  variant: 'success' | 'error'
}

type PolicyFormState = {
  autonomyCeiling: string
  autonomyConfidence: string
}

function toFormState(settings: PolicySettings): PolicyFormState {
  return {
    autonomyCeiling: String(settings.autonomyCeiling),
    autonomyConfidence: String(settings.autonomyConfidence),
  }
}

function validateForm(form: PolicyFormState): string | null {
  const ceiling = Number.parseFloat(form.autonomyCeiling)
  if (!form.autonomyCeiling.trim() || Number.isNaN(ceiling) || ceiling <= 0) {
    return 'Auto-approval ceiling must be greater than zero.'
  }

  const confidence = Number.parseFloat(form.autonomyConfidence)
  if (
    !form.autonomyConfidence.trim() ||
    Number.isNaN(confidence) ||
    confidence < 0 ||
    confidence > 1
  ) {
    return 'Confidence threshold must be between 0 and 1.'
  }

  return null
}

export function PolicySettingsPage() {
  const role = useAppRole()
  const [settings, setSettings] = useState<PolicySettings | null>(null)
  const [form, setForm] = useState<PolicyFormState>({ autonomyCeiling: '', autonomyConfidence: '' })
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [toast, setToast] = useState<ToastState | null>(null)

  const refreshSettings = useCallback(async () => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const current = await loadPolicySettings()
      setSettings(current)
      setForm(toFormState(current))
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Failed to load policy settings.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    if (role === 'Admin') {
      void refreshSettings()
    }
  }, [role, refreshSettings])

  useEffect(() => {
    if (!toast) {
      return undefined
    }

    const timeoutId = window.setTimeout(() => setToast(null), 4000)
    return () => window.clearTimeout(timeoutId)
  }, [toast])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const validationError = validateForm(form)
    if (validationError) {
      setToast({ message: validationError, variant: 'error' })
      return
    }

    setIsSaving(true)

    try {
      const updated = await updatePolicySettings({
        autonomyCeiling: Number.parseFloat(form.autonomyCeiling),
        autonomyConfidence: Number.parseFloat(form.autonomyConfidence),
      })

      setSettings(updated)
      setForm(toFormState(updated))
      setToast({
        message: 'Policy settings updated. Changes apply to new submissions immediately.',
        variant: 'success',
      })
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? (error.details?.join(' ') ?? error.message)
          : 'Failed to update policy settings.'

      setToast({ message, variant: 'error' })
    } finally {
      setIsSaving(false)
    }
  }

  if (role !== 'Admin') {
    return (
      <section className="rounded-xl border border-amber-200 bg-amber-50 p-6 shadow-sm">
        <h2 className="text-xl font-semibold text-amber-900">Admin access required</h2>
        <p className="mt-2 text-sm text-amber-800">
          Policy settings are available only to the Admin role. Switch roles using the selector in
          the top bar.
        </p>
      </section>
    )
  }

  return (
    <>
      {toast ? (
        <Toast
          message={toast.message}
          variant={toast.variant}
          onDismiss={() => setToast(null)}
        />
      ) : null}

      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
            Policy Settings
          </h2>
          <p className="mt-1 text-sm text-slate-600">
            Tune auto-approval thresholds. Expenses above the ceiling or below the confidence
            threshold escalate to the Approvals Queue.
          </p>
        </div>

        <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          {errorMessage ? (
            <div className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
              <p>{errorMessage}</p>
              <button
                type="button"
                onClick={() => void refreshSettings()}
                className="mt-2 text-xs font-semibold uppercase tracking-wide text-red-700 underline"
              >
                Try again
              </button>
            </div>
          ) : null}

          {isLoading ? (
            <p className="text-sm text-slate-500">Loading policy settings…</p>
          ) : (
            <form className="max-w-xl space-y-6" onSubmit={(event) => void handleSubmit(event)}>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
                <span className="font-medium text-slate-900">Policy version:</span>{' '}
                {settings?.policyVersion ?? '—'}
              </div>

              <div>
                <label
                  htmlFor="autonomyCeiling"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  Auto-approval ceiling (USD)
                </label>
                <input
                  id="autonomyCeiling"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={form.autonomyCeiling}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      autonomyCeiling: event.target.value,
                    }))
                  }
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
                />
                <p className="mt-1.5 text-xs text-slate-500">
                  Expenses above this amount always escalate for human review (AUTONOMY-CEILING).
                </p>
              </div>

              <div>
                <label
                  htmlFor="autonomyConfidence"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  Minimum AI confidence (0–1)
                </label>
                <input
                  id="autonomyConfidence"
                  type="number"
                  min="0"
                  max="1"
                  step="0.01"
                  value={form.autonomyConfidence}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      autonomyConfidence: event.target.value,
                    }))
                  }
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
                />
                <p className="mt-1.5 text-xs text-slate-500">
                  AI must meet or exceed this confidence score to auto-approve without escalation.
                </p>
              </div>

              <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                Changes apply to new submissions immediately. Existing escalated items are not
                re-evaluated.
              </div>

              <button
                type="submit"
                disabled={isSaving}
                className="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-300 focus:ring-offset-2 disabled:cursor-not-allowed disabled:bg-blue-400"
              >
                {isSaving ? 'Saving…' : 'Update Policy'}
              </button>
            </form>
          )}
        </section>
      </div>
    </>
  )
}
