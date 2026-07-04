import type { UserRole } from '../../types/userRole'
import { RoleBadge } from './RoleBadge'
import { RoleSwitcher } from './RoleSwitcher'

type TopNavbarProps = {
  role: UserRole
  onRoleChange: (role: UserRole) => void
}

export function TopNavbar({ role, onRoleChange }: TopNavbarProps) {
  return (
    <header className="flex min-h-16 flex-wrap items-center justify-between gap-4 border-b border-slate-200 bg-white px-8 py-3">
      <div>
        <h1 className="text-base font-semibold text-slate-900">Workspace</h1>
        <p className="text-sm text-slate-500">Manage submissions and approvals</p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <RoleSwitcher role={role} onRoleChange={onRoleChange} />
        <RoleBadge role={role} />
      </div>
    </header>
  )
}
