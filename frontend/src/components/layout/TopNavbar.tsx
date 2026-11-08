import { useNavigate } from 'react-router-dom'
import { getRoleDescription, type UserRole } from '../../types/userRole'
import { RoleBadge } from './RoleBadge'
import { SidebarToggleButton } from './SidebarToggleButton'

type TopNavbarProps = {
  role: UserRole
  username: string
  onLogout: () => void
  isSidebarOpen: boolean
  onOpenSidebar: () => void
}

export function TopNavbar({
  role,
  username,
  onLogout,
  isSidebarOpen,
  onOpenSidebar,
}: TopNavbarProps) {
  const navigate = useNavigate()

  function handleLogout() {
    onLogout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="flex min-h-16 flex-wrap items-center justify-between gap-4 border-b border-slate-200 bg-white px-8 py-3">
      <div className="flex min-w-0 items-start gap-3">
        {!isSidebarOpen ? (
          <SidebarToggleButton label="Open sidebar" onClick={onOpenSidebar} className="mt-0.5" />
        ) : null}
        <div>
          <h1 className="text-base font-semibold text-slate-900">Workspace</h1>
          <p className="text-sm text-slate-500">
            {username} · {getRoleDescription(role)}
          </p>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <RoleBadge role={role} />
        <button
          type="button"
          onClick={handleLogout}
          className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
        >
          Sign out
        </button>
      </div>
    </header>
  )
}
