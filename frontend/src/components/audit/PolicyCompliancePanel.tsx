import type { AuditTrailResponse } from '../../types/api'

type PolicyCompliancePanelProps = {
  audit: AuditTrailResponse
  currentAutonomyCeiling?: number | null
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

function wasAutoApproved(audit: AuditTrailResponse): boolean {
  return audit.timeline.some(
    (event) =>
      event.step === 'PolicyDecision' &&
      event.summary.toLowerCase().includes('auto-approved'),
  )
}

function hasCeilingRule(audit: AuditTrailResponse): boolean {
  return audit.rulesApplied.some((rule) => rule.toUpperCase() === 'AUTONOMY-CEILING')
}

export function PolicyCompliancePanel({
  audit,
  currentAutonomyCeiling = null,
}: PolicyCompliancePanelProps) {
  const amount = audit.invoice.amount
  const ceiling =
    audit.autonomyCeilingAtDecision ??
    currentAutonomyCeiling ??
    null
  const autoApproved = wasAutoApproved(audit)
  const escalatedForCeiling = hasCeilingRule(audit)
  const withinCeiling = ceiling != null ? amount <= ceiling : null

  let tone: 'success' | 'warning' | 'danger' = 'success'
  let title = 'Policy limit compliance'
  let message =
    'This item was processed under the configured autonomy rules. Review amount versus ceiling below.'

  if (ceiling == null) {
    tone = 'warning'
    message =
      'Autonomy ceiling at decision time was not recorded. Compare the submission amount against the policy version shown below.'
  } else if (autoApproved && withinCeiling) {
    tone = 'success'
    title = 'Compliance verified — auto-approved within limit'
    message = `Auto-approval is allowed because ${formatCurrency(amount)} is at or below the autonomy ceiling of ${formatCurrency(ceiling)} that applied at decision time (${audit.policyVersion}). The deterministic policy engine made the final decision.`
  } else if (autoApproved && !withinCeiling) {
    tone = 'danger'
    title = 'Policy violation — auto-approved above ceiling'
    message = `This submission was auto-approved at ${formatCurrency(amount)} even though the ceiling at decision time was ${formatCurrency(ceiling)}. This should not happen and requires investigation.`
  } else if (escalatedForCeiling) {
    tone = 'success'
    title = 'Compliance verified — escalated above ceiling'
    message = `Amount ${formatCurrency(amount)} exceeded the autonomy ceiling of ${formatCurrency(ceiling)} at decision time. The AUTONOMY-CEILING rule applied and the item was escalated instead of auto-approved.`
  } else if (!autoApproved && withinCeiling === false) {
    tone = 'warning'
    title = 'Human or policy review above ceiling'
    message = `Amount ${formatCurrency(amount)} is above the ceiling of ${formatCurrency(ceiling)} at decision time. Confirm the final decision trail shows escalation or human approval rather than autonomous approval.`
  }

  const toneStyles = {
    success: 'border-emerald-200 bg-emerald-50 text-emerald-950',
    warning: 'border-amber-200 bg-amber-50 text-amber-950',
    danger: 'border-red-200 bg-red-50 text-red-950',
  } as const

  return (
    <section className={`rounded-xl border p-5 shadow-sm ${toneStyles[tone]}`}>
      <h3 className="text-base font-semibold">{title}</h3>
      <p className="mt-2 text-sm">{message}</p>
      <dl className="mt-4 grid grid-cols-1 gap-3 text-sm sm:grid-cols-3">
        <div>
          <dt className="font-medium opacity-80">Submission amount</dt>
          <dd className="text-lg font-semibold">{formatCurrency(amount)}</dd>
        </div>
        <div>
          <dt className="font-medium opacity-80">Ceiling at decision</dt>
          <dd className="text-lg font-semibold">
            {ceiling != null ? formatCurrency(ceiling) : '—'}
          </dd>
        </div>
        <div>
          <dt className="font-medium opacity-80">Policy version</dt>
          <dd className="text-lg font-semibold">{audit.policyVersion}</dd>
        </div>
      </dl>
      {escalatedForCeiling ? (
        <p className="mt-3 text-xs font-semibold uppercase tracking-wide opacity-80">
          Rule applied: AUTONOMY-CEILING
        </p>
      ) : null}
    </section>
  )
}
