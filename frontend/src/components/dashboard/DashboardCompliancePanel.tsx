import { Link } from 'react-router-dom'
import type { ComplianceReportResponse } from '../../types/api'
import { MobileDataCard, MobileDataList, MobileDataRow } from '../ui/MobileDataCard'

type DashboardCompliancePanelProps = {
  report: ComplianceReportResponse
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)
}

export function DashboardCompliancePanel({ report }: DashboardCompliancePanelProps) {
  const isClean = report.violationCount === 0

  return (
    <section
      className={[
        'rounded-xl border p-4 shadow-sm sm:p-6',
        isClean ? 'border-emerald-200 bg-emerald-50' : 'border-red-200 bg-red-50',
      ].join(' ')}
    >
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">Auto-approval limit compliance</h3>
          <p className="mt-1 text-sm text-slate-700">
            Spot-check of every auto-approved submission against the configured ceiling (
            {formatCurrency(report.autonomyCeiling)}, {report.policyVersion}).
          </p>
        </div>
        <div className="text-right">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-600">Violations</p>
          <p
            className={[
              'text-2xl font-bold',
              isClean ? 'text-emerald-800' : 'text-red-800',
            ].join(' ')}
          >
            {report.violationCount}
          </p>
        </div>
      </div>

      {report.autoApprovedItems.length === 0 ? (
        <p className="mt-4 text-sm text-slate-600">No auto-approved submissions to review yet.</p>
      ) : (
        <>
          <div className="mt-4 space-y-3 md:hidden">
            {report.autoApprovedItems.map((item) => (
              <MobileDataCard key={item.trackingId}>
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <p className="font-mono text-sm font-semibold text-slate-900">{item.invoiceNumber}</p>
                  <span
                    className={[
                      'inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset',
                      item.isViolation
                        ? 'bg-red-100 text-red-800 ring-red-200'
                        : 'bg-emerald-100 text-emerald-800 ring-emerald-200',
                    ].join(' ')}
                  >
                    {item.isViolation ? 'Above ceiling' : 'Within limit'}
                  </span>
                </div>
                <MobileDataList className="mt-3">
                  <MobileDataRow label="Vendor" value={item.vendor} />
                  <MobileDataRow label="Amount" value={formatCurrency(item.amount)} />
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

          <div className="mt-4 hidden overflow-x-auto rounded-lg border border-white/60 bg-white/70 md:block">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Invoice
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Vendor
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Amount
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Status
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Audit
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {report.autoApprovedItems.map((item) => (
                  <tr key={item.trackingId}>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-slate-900">
                      {item.invoiceNumber}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-700">{item.vendor}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-medium text-slate-900">
                      {formatCurrency(item.amount)}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <span
                        className={[
                          'inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset',
                          item.isViolation
                            ? 'bg-red-100 text-red-800 ring-red-200'
                            : 'bg-emerald-100 text-emerald-800 ring-emerald-200',
                        ].join(' ')}
                      >
                        {item.isViolation ? 'Above ceiling' : 'Within limit'}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
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
  )
}
