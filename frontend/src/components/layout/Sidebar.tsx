import { NavLink } from 'react-router-dom'
import { getNavItemsForRole, type UserRole } from '../../types/userRole'

type SidebarProps = {
  role: UserRole
  isOpen: boolean
  onClose: () => void
}

function CloseIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className="h-5 w-5"
    >
      <path d="M18 6 6 18M6 6l12 12" />
    </svg>
  )
}

export function Sidebar({ role, isOpen, onClose }: SidebarProps) {
  const navItems = getNavItemsForRole(role)

  return (
    <aside
      className={[
        'flex shrink-0 flex-col border-r border-slate-200 bg-white transition-[width] duration-200 ease-in-out',
        isOpen ? 'w-64' : 'w-0 overflow-hidden border-r-0',
      ].join(' ')}
      aria-hidden={!isOpen}
    >
      <div className="flex w-64 flex-col">
        <div className="flex items-start justify-between gap-3 border-b border-slate-200 px-6 py-5">
          <div className="min-w-0">
            <p className="text-lg font-semibold tracking-tight text-slate-900">ApprovalFlow</p>
            <p className="mt-1 text-sm text-slate-500">Expense approval platform</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close sidebar"
            title="Close sidebar"
            className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-300"
          >
            <CloseIcon />
          </button>
        </div>

        <nav className="flex flex-1 flex-col gap-1 p-4" aria-label="Main navigation">
          {navItems.map(({ label, to }) => (
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
      </div>
    </aside>
  )
}
