import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { DashboardOperationsChart } from '../components/dashboard/DashboardOperationsChart'
import { useAppRole } from '../hooks/useAppRole'
import { ApiRequestError } from '../services/apiClient'
import { loadReportingSummary } from '../services/reportService'
import type { ReportingSummaryResponse } from '../types/api'
import { isDashboardReadOnly } from '../types/userRole'

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

function formatRate(rate: number): string {
  return `${Math.round(rate * 100)}%`
}

function formatTimestamp(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '—'
  }

  return new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(parsed)
}

type MetricCardProps = {
  label: string
  value: string
  hint?: string
}

function MetricCard({ label, value, hint }: MetricCardProps) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <p className="text-sm font-medium text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-slate-900">{value}</p>
      {hint ? <p className="mt-1 text-xs text-slate-500">{hint}</p> : null}
    </div>
  )
}

export function DashboardPage() {
  const role = useAppRole()
  const readOnly = isDashboardReadOnly(role)
  const [summary, setSummary] = useState<ReportingSummaryResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const refreshSummary = useCallback(async () => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const data = await loadReportingSummary()
      setSummary(data)
    } catch (error) {
      const message =
        error instanceof ApiRequestError
          ? error.message
          : 'Failed to load approval operations summary.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void refreshSummary()
  }, [refreshSummary])

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
            Approval Operations
          </h2>
          <p className="mt-1 text-sm text-slate-600">
            Throughput, auto-approval performance, and recent activity across the platform.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void refreshSummary()}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 shadow-sm transition-colors hover:bg-slate-50"
        >
          Refresh
        </button>
      </div>

      {readOnly ? (
        <div className="rounded-lg border border-slate-200 bg-slate-100 px-4 py-3 text-sm text-slate-700">
          Auditor view: metrics are read-only. Use the audit trail to inspect individual
          submissions.
        </div>
      ) : null}

      {errorMessage ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {errorMessage}
        </div>
      ) : null}

      {isLoading ? (
        <p className="text-sm text-slate-500">Loading dashboard metrics…</p>
      ) : summary ? (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <MetricCard label="Total submissions" value={String(summary.totalSubmissions)} />
            <MetricCard
              label="Auto-approval rate"
              value={formatRate(summary.autoApprovalRate)}
              hint={`${summary.autoApprovedCount} auto-approved`}
            />
            <MetricCard
              label="Escalation rate"
              value={formatRate(summary.escalationRate)}
              hint={`${summary.escalatedCount} escalated`}
            />
            <MetricCard
              label="Paid submissions"
              value={String(summary.paidCount)}
              hint={`${summary.paymentFailedCount} payment failures`}
            />
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <MetricCard
              label="Auto-approved amount"
              value={formatCurrency(summary.autoApprovedAmount)}
            />
            <MetricCard
              label="Human-approved amount"
              value={formatCurrency(summary.humanApprovedAmount)}
              hint={`${summary.humanApprovedCount} human approvals · ${summary.rejectedCount} rejections`}
            />
          </div>

          <DashboardOperationsChart summary={summary} />

          <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="border-b border-slate-200 px-6 py-5">
              <h3 className="text-lg font-semibold text-slate-900">Recent Activity</h3>
            </div>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Invoice
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Vendor
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Amount
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Decision
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Recorded
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Audit
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 bg-white">
                  {summary.recentActivity.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-10 text-center text-sm text-slate-500">
                        No submissions yet.
                      </td>
                    </tr>
                  ) : (
                    summary.recentActivity.map((item) => (
                      <tr key={item.trackingId}>
                        <td className="whitespace-nowrap px-6 py-4 font-mono text-sm text-slate-900">
                          {item.invoiceNumber}
                        </td>
                        <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                          {item.vendor}
                        </td>
                        <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-slate-900">
                          {formatCurrency(item.amount)}
                        </td>
                        <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                          {item.status}
                        </td>
                        <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-700">
                          {item.decisionType}
                        </td>
                        <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-500">
                          {formatTimestamp(item.recordedAt)}
                        </td>
                        <td className="whitespace-nowrap px-6 py-4 text-sm">
                          <Link
                            to={`/audit?trackingId=${encodeURIComponent(item.trackingId)}`}
                            className="font-medium text-blue-700 underline hover:text-blue-900"
                          >
                            View trail
                          </Link>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </>
      ) : null}
    </div>
  )
}
