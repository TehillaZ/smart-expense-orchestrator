import { FormEvent, useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PolicyCompliancePanel } from '../components/audit/PolicyCompliancePanel'
import { ApiRequestError } from '../services/apiClient'
import { loadAuditTrail } from '../services/auditService'
import { loadPolicySettings } from '../services/policyService'
import type { AuditTrailResponse, PolicySettings } from '../types/api'

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

export function AuditTrailPage() {
  const [searchParams] = useSearchParams()
  const [identifier, setIdentifier] = useState('')
  const [audit, setAudit] = useState<AuditTrailResponse | null>(null)
  const [policy, setPolicy] = useState<PolicySettings | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const runSearch = useCallback(async (id: string) => {
    const trimmedId = id.trim()
    if (!trimmedId) {
      setErrorMessage('Enter a tracking id or correlation id to search.')
      return
    }

    setIsLoading(true)
    setErrorMessage(null)
    setAudit(null)
    setPolicy(null)

    try {
      const [trail, policySettings] = await Promise.all([
        loadAuditTrail(trimmedId),
        loadPolicySettings(),
      ])
      setAudit(trail)
      setPolicy(policySettings)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Failed to load the audit trail.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    const fromTracking = searchParams.get('trackingId')
    const fromCorrelation = searchParams.get('correlationId')
    const initial = fromTracking ?? fromCorrelation

    if (initial) {
      setIdentifier(initial)
      void runSearch(initial)
    }
  }, [searchParams, runSearch])

  async function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await runSearch(identifier)
  }

  async function copyCorrelationId() {
    if (!audit?.correlationId) {
      return
    }

    await navigator.clipboard.writeText(audit.correlationId)
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Audit Trail</h2>
        <p className="mt-1 text-sm text-slate-600">
          Search by tracking id or correlation id to inspect policy rules, AI reasoning, human
          decisions, payment outcomes, and autonomy limit compliance.
        </p>
      </div>

      <form
        className="flex flex-wrap items-end gap-3 rounded-xl border border-slate-200 bg-white p-6 shadow-sm"
        onSubmit={(event) => void handleSearch(event)}
      >
        <div className="min-w-[16rem] flex-1">
          <label htmlFor="audit-identifier" className="mb-1.5 block text-sm font-medium text-slate-700">
            Tracking ID or Correlation ID
          </label>
          <input
            id="audit-identifier"
            value={identifier}
            onChange={(event) => setIdentifier(event.target.value)}
            placeholder="Paste a tracking id or correlation id"
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <button
          type="submit"
          disabled={isLoading}
          className="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-400"
        >
          {isLoading ? 'Searching…' : 'Search'}
        </button>
      </form>

      {errorMessage ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {errorMessage}
        </div>
      ) : null}

      {audit ? (
        <>
          <PolicyCompliancePanel
            audit={audit}
            currentAutonomyCeiling={policy?.autonomyCeiling ?? null}
          />

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-1">
              <h3 className="text-lg font-semibold text-slate-900">Submission</h3>
              <dl className="mt-4 space-y-3 text-sm">
                <div>
                  <dt className="font-medium text-slate-500">Vendor</dt>
                  <dd className="text-slate-900">{audit.invoice.vendor}</dd>
                </div>
                <div>
                  <dt className="font-medium text-slate-500">Invoice</dt>
                  <dd className="font-mono text-slate-900">{audit.invoice.invoiceNumber}</dd>
                </div>
                <div>
                  <dt className="font-medium text-slate-500">Amount</dt>
                  <dd className="text-slate-900">{formatCurrency(audit.invoice.amount)}</dd>
                </div>
                <div>
                  <dt className="font-medium text-slate-500">Category</dt>
                  <dd className="text-slate-900">{audit.invoice.category}</dd>
                </div>
                <div>
                  <dt className="font-medium text-slate-500">Policy version</dt>
                  <dd className="text-slate-900">{audit.policyVersion}</dd>
                </div>
                <div>
                  <dt className="font-medium text-slate-500">Correlation ID</dt>
                  <dd className="flex items-center gap-2">
                    <span className="break-all font-mono text-xs text-slate-900">
                      {audit.correlationId}
                    </span>
                    <button
                      type="button"
                      onClick={() => void copyCorrelationId()}
                      className="rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50"
                    >
                      Copy
                    </button>
                  </dd>
                </div>
              </dl>
            </section>

            <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2">
              <h3 className="text-lg font-semibold text-slate-900">Decision Context</h3>
              <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
                <div className="rounded-lg border border-violet-200 bg-violet-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-wide text-violet-700">
                    AI Recommendation
                  </p>
                  <p className="mt-2 text-sm font-medium text-violet-900">
                    {audit.ai.recommendation ?? '—'}
                  </p>
                  <p className="mt-1 text-sm text-violet-800">
                    Confidence:{' '}
                    {audit.ai.confidence != null
                      ? `${Math.round(audit.ai.confidence * 100)}%`
                      : '—'}
                  </p>
                  <p className="mt-2 text-sm text-violet-900">{audit.ai.reasoningSummary}</p>
                  {audit.rulesApplied.length > 0 ? (
                    <p className="mt-2 text-xs text-violet-800">
                      Rules: {audit.rulesApplied.join(', ')}
                    </p>
                  ) : null}
                </div>

                <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-600">
                    Human Decision
                  </p>
                  {audit.humanDecision ? (
                    <>
                      <p className="mt-2 text-sm font-medium text-slate-900">
                        {audit.humanDecision.decision} by {audit.humanDecision.actor}
                      </p>
                      {audit.humanDecision.comment ? (
                        <p className="mt-1 text-sm text-slate-700">
                          {audit.humanDecision.comment}
                        </p>
                      ) : null}
                    </>
                  ) : (
                    <p className="mt-2 text-sm text-slate-600">No human decision recorded.</p>
                  )}
                </div>
              </div>

              {audit.payment ? (
                <div className="mt-4 rounded-lg border border-emerald-200 bg-emerald-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
                    Payment Result
                  </p>
                  <p className="mt-2 text-sm font-medium text-emerald-900">{audit.payment.status}</p>
                  <p className="mt-1 text-sm text-emerald-800">{audit.payment.reason}</p>
                </div>
              ) : null}

              <div className="mt-6">
                <h4 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
                  Timeline
                </h4>
                <ol className="mt-4 space-y-4 border-l border-slate-200 pl-4">
                  {audit.timeline.map((event) => (
                    <li key={`${event.step}-${event.summary}`} className="relative">
                      <span className="absolute -left-[1.35rem] top-1 h-2.5 w-2.5 rounded-full bg-slate-900" />
                      <p className="text-sm font-semibold text-slate-900">{event.step}</p>
                      <p className="text-sm text-slate-700">{event.summary}</p>
                      <p className="text-xs text-slate-500">{event.actor}</p>
                      {event.detail ? (
                        <p className="mt-1 text-xs text-slate-600">{event.detail}</p>
                      ) : null}
                    </li>
                  ))}
                </ol>
              </div>
            </section>
          </div>
        </>
      ) : null}
    </div>
  )
}
