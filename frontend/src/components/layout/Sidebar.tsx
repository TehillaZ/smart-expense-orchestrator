import { NavLink } from 'react-router-dom'

const NAV_ITEMS = [
  { label: 'My Submissions', to: '/submissions' },
  { label: 'Approvals Queue', to: '/approvals' },
  { label: 'Policy Settings', to: '/policy' },
] as const

export function Sidebar() {
  return (
    <aside className="flex w-64 shrink-0 flex-col border-r border-slate-200 bg-white">
      <div className="border-b border-slate-200 px-6 py-5">
        <p className="text-lg font-semibold tracking-tight text-slate-900">
          ApprovalFlow
        </p>
        <p className="mt-1 text-sm text-slate-500">Expense approval platform</p>
      </div>

      <nav className="flex flex-1 flex-col gap-1 p-4" aria-label="Main navigation">
        {NAV_ITEMS.map(({ label, to }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              [
                'rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-slate-900 text-white'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
              ].join(' ')
            }
          >
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
