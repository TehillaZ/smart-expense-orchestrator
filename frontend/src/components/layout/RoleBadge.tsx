import type { UserRole } from '../../types/userRole'

const ROLE_STYLES: Record<UserRole, string> = {
  Submitter: 'bg-sky-100 text-sky-800 ring-sky-200',
  Approver: 'bg-violet-100 text-violet-800 ring-violet-200',
  Admin: 'bg-amber-100 text-amber-900 ring-amber-200',
}

type RoleBadgeProps = {
  role: UserRole
}

export function RoleBadge({ role }: RoleBadgeProps) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold uppercase tracking-wide ring-1 ring-inset ${ROLE_STYLES[role]}`}
    >
      {role}
    </span>
  )
}
