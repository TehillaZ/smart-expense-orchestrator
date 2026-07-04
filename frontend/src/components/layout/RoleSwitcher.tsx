import { USER_ROLES, type UserRole } from '../../types/userRole'

type RoleSwitcherProps = {
  role: UserRole
  onRoleChange: (role: UserRole) => void
}

export function RoleSwitcher({ role, onRoleChange }: RoleSwitcherProps) {
  return (
    <div
      className="inline-flex rounded-lg border border-slate-200 bg-slate-100 p-1 shadow-sm"
      role="group"
      aria-label="Switch demo role"
    >
      {USER_ROLES.map((option) => {
        const isActive = role === option

        return (
          <button
            key={option}
            type="button"
            aria-pressed={isActive}
            onClick={() => onRoleChange(option)}
            className={[
              'rounded-md px-3 py-1.5 text-xs font-semibold transition-all',
              isActive
                ? 'bg-white text-slate-900 shadow-sm ring-1 ring-slate-200'
                : 'text-slate-600 hover:text-slate-900',
            ].join(' ')}
          >
            {option}
          </button>
        )
      })}
    </div>
  )
}
