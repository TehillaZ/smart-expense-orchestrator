import { getRoleDescription, type UserRole } from '../../types/userRole'
import { RoleBadge } from './RoleBadge'
import { RoleSwitcher } from './RoleSwitcher'
import { SidebarToggleButton } from './SidebarToggleButton'

type TopNavbarProps = {
  role: UserRole
  onRoleChange: (role: UserRole) => void
  isSidebarOpen: boolean
  onOpenSidebar: () => void
}

export function TopNavbar({
  role,
  onRoleChange,
  isSidebarOpen,
  onOpenSidebar,
}: TopNavbarProps) {
  return (
    <header className="flex min-h-16 flex-wrap items-center justify-between gap-4 border-b border-slate-200 bg-white px-8 py-3">
      <div className="flex min-w-0 items-start gap-3">
        {!isSidebarOpen ? (
          <SidebarToggleButton label="Open sidebar" onClick={onOpenSidebar} className="mt-0.5" />
        ) : null}
        <div>
          <h1 className="text-base font-semibold text-slate-900">Workspace</h1>
          <p className="text-sm text-slate-500">{getRoleDescription(role)}</p>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <RoleSwitcher role={role} onRoleChange={onRoleChange} />
        <RoleBadge role={role} />
      </div>
    </header>
  )
}
