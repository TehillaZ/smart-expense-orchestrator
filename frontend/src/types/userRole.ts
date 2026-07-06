export const USER_ROLES = ['Submitter', 'Approver', 'Admin'] as const

export type UserRole = (typeof USER_ROLES)[number]

export type NavItem = {
  label: string
  to: '/submissions' | '/approvals' | '/policy' | '/dashboard' | '/audit'
  to: '/submissions' | '/approvals' | '/policy'
  roles: readonly UserRole[]
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'My Submissions', to: '/submissions', roles: ['Submitter', 'Admin'] },
  { label: 'Approvals Queue', to: '/approvals', roles: ['Approver', 'Admin'] },
  { label: 'Dashboard', to: '/dashboard', roles: ['Admin'] },
  { label: 'Audit Trail', to: '/audit', roles: ['Admin'] },
  { label: 'Policy Settings', to: '/policy', roles: ['Admin'] },
] as const

export function isUserRole(value: string): value is UserRole {
  return USER_ROLES.includes(value as UserRole)
}

export function getNavItemsForRole(role: UserRole): NavItem[] {
  return NAV_ITEMS.filter((item) => item.roles.includes(role))
}

export function canAccessPath(role: UserRole, pathname: string): boolean {
  const path = pathname.replace(/\/$/, '') || '/submissions'

  return NAV_ITEMS.some((item) => item.to === path && item.roles.includes(role))
}

export function getDefaultRoute(role: UserRole): NavItem['to'] {
  const items = getNavItemsForRole(role)
  return items[0]?.to ?? '/submissions'
}

export function canSubmitExpenses(role: UserRole): boolean {
  return role === 'Submitter'
}

export function canDecideEscalations(role: UserRole): boolean {
  return role === 'Approver' || role === 'Admin'
}

export function getRoleDescription(role: UserRole): string {
  switch (role) {
    case 'Submitter':
      return 'Submit expenses and track approval outcomes.'
    case 'Approver':
      return 'Review escalated expenses and approve or reject them.'
    case 'Admin':
      return 'Full access: submissions, approvals, dashboard, audit trail, and policy settings.'
    default: {
      const _exhaustive: never = role
      return _exhaustive
    }
  }
}
