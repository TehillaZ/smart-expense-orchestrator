import { EXPENSE_CATEGORIES } from '../../types/submission'
import type { EscalationQueueFilters } from '../../utils/escalationQueueFilters'

type EscalationQueueFiltersBarProps = {
  filters: EscalationQueueFilters
  onChange: (filters: EscalationQueueFilters) => void
  onReset: () => void
  resultCount: number
  totalCount: number
}

export function EscalationQueueFiltersBar({
  filters,
  onChange,
  onReset,
  resultCount,
  totalCount,
}: EscalationQueueFiltersBarProps) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-slate-900">Filter queue</h3>
          <p className="mt-0.5 text-xs text-slate-500">
            Showing {resultCount} of {totalCount} escalated items
          </p>
        </div>
        <button
          type="button"
          onClick={onReset}
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
        >
          Clear filters
        </button>
      </div>

      <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-5">
        <div className="xl:col-span-2">
          <label htmlFor="queue-search" className="mb-1 block text-xs font-medium text-slate-600">
            Search vendor, invoice, or tracking id
          </label>
          <input
            id="queue-search"
            type="search"
            value={filters.searchText}
            onChange={(event) =>
              onChange({ ...filters, searchText: event.target.value })
            }
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
          />
        </div>

        <div>
          <label htmlFor="queue-category" className="mb-1 block text-xs font-medium text-slate-600">
            Category
          </label>
          <select
            id="queue-category"
            value={filters.category}
            onChange={(event) => onChange({ ...filters, category: event.target.value })}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
          >
            <option value="">All categories</option>
            {EXPENSE_CATEGORIES.map((category) => (
              <option key={category} value={category}>
                {category}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="queue-min-amount" className="mb-1 block text-xs font-medium text-slate-600">
            Min amount
          </label>
          <input
            id="queue-min-amount"
            type="number"
            min="0"
            step="0.01"
            value={filters.minAmount}
            onChange={(event) => onChange({ ...filters, minAmount: event.target.value })}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
          />
        </div>

        <div>
          <label htmlFor="queue-max-amount" className="mb-1 block text-xs font-medium text-slate-600">
            Max amount
          </label>
          <input
            id="queue-max-amount"
            type="number"
            min="0"
            step="0.01"
            value={filters.maxAmount}
            onChange={(event) => onChange({ ...filters, maxAmount: event.target.value })}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
          />
        </div>
      </div>

      <label className="mt-3 inline-flex items-center gap-2 text-sm text-slate-700">
        <input
          type="checkbox"
          checked={filters.infoRequestedOnly}
          onChange={(event) =>
            onChange({ ...filters, infoRequestedOnly: event.target.checked })
          }
          className="rounded border-slate-300 text-blue-600 focus:ring-blue-300"
        />
        Info requested only
      </label>
    </section>
  )
}
