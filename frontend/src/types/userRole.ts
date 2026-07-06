export const USER_ROLES = ['Submitter', 'Approver', 'Controller', 'Auditor'] as const

export type UserRole = (typeof USER_ROLES)[number]

export type NavItem = {
  label: string
  to: '/submissions' | '/approvals' | '/policy' | '/dashboard' | '/audit'
  roles: readonly UserRole[]
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Submit', to: '/submissions', roles: ['Submitter'] },
  { label: 'Approvals', to: '/approvals', roles: ['Approver'] },
  { label: 'Dashboard', to: '/dashboard', roles: ['Controller', 'Auditor'] },
  { label: 'Audit Trail', to: '/audit', roles: ['Controller', 'Auditor'] },
  { label: 'Policy', to: '/policy', roles: ['Controller'] },
] as const

export function isUserRole(value: string): value is UserRole {
  return USER_ROLES.includes(value as UserRole)
}

export function getNavItemsForRole(role: UserRole): NavItem[] {
  return NAV_ITEMS.filter((item) => item.roles.includes(role))
}

function normalizePath(pathname: string): string {
  const trimmed = pathname.replace(/\/$/, '')
  return trimmed.length === 0 ? '/submissions' : trimmed
}

export function canAccessPath(role: UserRole, pathname: string): boolean {
  const path = normalizePath(pathname)

  if (path === '/submissions' || path.startsWith('/submissions/')) {
    return role === 'Submitter'
  }

  if (path === '/approvals' || path.startsWith('/approvals/')) {
    return role === 'Approver'
  }

  if (path === '/dashboard') {
    return role === 'Controller' || role === 'Auditor'
  }

  if (path === '/audit' || path.startsWith('/audit/')) {
    return role === 'Controller' || role === 'Auditor'
  }

  if (path === '/policy') {
    return role === 'Controller'
  }

  return false
}

export function getDefaultRoute(role: UserRole): NavItem['to'] {
  const items = getNavItemsForRole(role)
  return items[0]?.to ?? '/submissions'
}

export function canSubmitExpenses(role: UserRole): boolean {
  return role === 'Submitter'
}

export function canDecideEscalations(role: UserRole): boolean {
  return role === 'Approver'
}

export function canManagePolicy(role: UserRole): boolean {
  return role === 'Controller'
}

export function isDashboardReadOnly(role: UserRole): boolean {
  return role === 'Auditor'
}

export function canViewAuditTrail(role: UserRole): boolean {
  return role === 'Controller' || role === 'Auditor'
}

export function getRoleDescription(role: UserRole): string {
  switch (role) {
    case 'Submitter':
      return 'Submit expenses and track approval outcomes.'
    case 'Approver':
      return 'Review escalated expenses and approve, reject, or request more info.'
    case 'Controller':
      return 'Configure policy thresholds and monitor approval operations.'
    case 'Auditor':
      return 'Inspect audit trails and verify autonomy limit compliance.'
    default: {
      const _exhaustive: never = role
      return _exhaustive
    }
  }
}
