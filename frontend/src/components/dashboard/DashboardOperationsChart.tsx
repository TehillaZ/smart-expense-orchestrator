import type { ReportingSummaryResponse } from '../../types/api'

type DashboardOperationsChartProps = {
  summary: ReportingSummaryResponse
}

type ChartBar = {
  label: string
  value: number
  colorClass: string
}

export function DashboardOperationsChart({ summary }: DashboardOperationsChartProps) {
  const bars: ChartBar[] = [
    {
      label: 'Auto-approved',
      value: summary.autoApprovedCount,
      colorClass: 'bg-emerald-500',
    },
    {
      label: 'Human-approved',
      value: summary.humanApprovedCount,
      colorClass: 'bg-teal-500',
    },
    {
      label: 'Escalated',
      value: summary.escalatedCount,
      colorClass: 'bg-violet-500',
    },
    {
      label: 'Rejected',
      value: summary.rejectedCount,
      colorClass: 'bg-red-500',
    },
    {
      label: 'Payment failed',
      value: summary.paymentFailedCount,
      colorClass: 'bg-orange-500',
    },
  ]

  const maxValue = Math.max(...bars.map((bar) => bar.value), 1)

  return (
    <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <h3 className="text-lg font-semibold text-slate-900">Decision outcomes</h3>
      <p className="mt-1 text-sm text-slate-500">
        Volume comparison across auto approvals, human decisions, escalations, and failures.
      </p>
      <div className="mt-6 space-y-4">
        {bars.map((bar) => (
          <div key={bar.label}>
            <div className="mb-1 flex items-center justify-between text-sm">
              <span className="font-medium text-slate-700">{bar.label}</span>
              <span className="text-slate-500">{bar.value}</span>
            </div>
            <div className="h-3 rounded-full bg-slate-100">
              <div
                className={`h-3 rounded-full ${bar.colorClass}`}
                style={{ width: `${(bar.value / maxValue) * 100}%` }}
              />
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
