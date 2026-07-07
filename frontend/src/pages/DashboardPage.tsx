import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { DashboardCompliancePanel } from '../components/dashboard/DashboardCompliancePanel'
import { DashboardOperationsChart } from '../components/dashboard/DashboardOperationsChart'
import { MobileDataCard, MobileDataList, MobileDataRow } from '../components/ui/MobileDataCard'
import { ApiRequestError } from '../services/apiClient'
import { loadComplianceReport, loadReportingSummary } from '../services/reportService'
import type { ComplianceReportResponse, ReportingSummaryResponse } from '../types/api'

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
  const [summary, setSummary] = useState<ReportingSummaryResponse | null>(null)
  const [compliance, setCompliance] = useState<ComplianceReportResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const refreshSummary = useCallback(async () => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [summaryData, complianceData] = await Promise.all([
        loadReportingSummary(),
        loadComplianceReport(),
      ])
      setSummary(summaryData)
      setCompliance(complianceData)
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

      {errorMessage ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {errorMessage}
        </div>
      ) : null}

      {isLoading ? (
        <p className="text-sm text-slate-500">Loading dashboard metrics…</p>
      ) : summary ? (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
            <MetricCard label="Total submissions" value={String(summary.totalSubmissions)} />
            <MetricCard
              label="Submissions today"
              value={String(summary.submissionsTodayCount)}
              hint="Received since UTC midnight"
            />
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

          {compliance ? <DashboardCompliancePanel report={compliance} /> : null}

          <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="border-b border-slate-200 px-4 py-4 sm:px-6 sm:py-5">
              <h3 className="text-lg font-semibold text-slate-900">Recent Activity</h3>
            </div>

            {summary.recentActivity.length === 0 ? (
              <p className="px-4 py-10 text-center text-sm text-slate-500 sm:px-6">
                No submissions yet.
              </p>
            ) : (
              <>
                <div className="space-y-3 p-4 md:hidden">
                  {summary.recentActivity.map((item) => (
                    <MobileDataCard key={item.trackingId}>
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <p className="font-mono text-sm font-semibold text-slate-900">
                          {item.invoiceNumber}
                        </p>
                        <p className="text-sm font-semibold text-slate-900">
                          {formatCurrency(item.amount)}
                        </p>
                      </div>
                      <MobileDataList className="mt-3">
                        <MobileDataRow label="Vendor" value={item.vendor} />
                        <MobileDataRow label="Status" value={item.status} />
                        <MobileDataRow label="Decision" value={item.decisionType} />
                        <MobileDataRow
                          label="Recorded"
                          value={formatTimestamp(item.recordedAt)}
                        />
                        <MobileDataRow
                          label="Audit"
                          value={
                            <Link
                              to={`/audit?trackingId=${encodeURIComponent(item.trackingId)}`}
                              className="font-medium text-blue-700 underline hover:text-blue-900"
                            >
                              View trail
                            </Link>
                          }
                        />
                      </MobileDataList>
                    </MobileDataCard>
                  ))}
                </div>

                <div className="hidden overflow-x-auto md:block">
                  <table className="min-w-full divide-y divide-slate-200">
                    <thead className="bg-slate-50">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6">
                          Invoice
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6">
                          Vendor
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6">
                          Amount
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6">
                          Status
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6">
                          Decision
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6">
                          Recorded
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 sm:px-6">
                          Audit
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 bg-white">
                      {summary.recentActivity.map((item) => (
                        <tr key={item.trackingId}>
                          <td className="whitespace-nowrap px-4 py-4 font-mono text-sm text-slate-900 sm:px-6">
                            {item.invoiceNumber}
                          </td>
                          <td className="whitespace-nowrap px-4 py-4 text-sm text-slate-700 sm:px-6">
                            {item.vendor}
                          </td>
                          <td className="whitespace-nowrap px-4 py-4 text-sm font-medium text-slate-900 sm:px-6">
                            {formatCurrency(item.amount)}
                          </td>
                          <td className="whitespace-nowrap px-4 py-4 text-sm text-slate-700 sm:px-6">
                            {item.status}
                          </td>
                          <td className="whitespace-nowrap px-4 py-4 text-sm text-slate-700 sm:px-6">
                            {item.decisionType}
                          </td>
                          <td className="whitespace-nowrap px-4 py-4 text-sm text-slate-500 sm:px-6">
                            {formatTimestamp(item.recordedAt)}
                          </td>
                          <td className="whitespace-nowrap px-4 py-4 text-sm sm:px-6">
                            <Link
                              to={`/audit?trackingId=${encodeURIComponent(item.trackingId)}`}
                              className="font-medium text-blue-700 underline hover:text-blue-900"
                            >
                              View trail
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </section>
        </>
      ) : null}
    </div>
  )
}
