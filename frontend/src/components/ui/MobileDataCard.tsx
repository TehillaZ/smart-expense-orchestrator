import type { ReactNode } from 'react'

type MobileDataCardProps = {
  children: ReactNode
  className?: string
  onClick?: () => void
}

export function MobileDataCard({ children, className = '', onClick }: MobileDataCardProps) {
  return (
    <article
      className={[
        'rounded-lg border border-slate-200 bg-white p-4 shadow-sm',
        onClick ? 'cursor-pointer transition-colors hover:bg-slate-50' : '',
        className,
      ].join(' ')}
      onClick={onClick}
      onKeyDown={
        onClick
          ? (event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault()
                onClick()
              }
            }
          : undefined
      }
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
    >
      {children}
    </article>
  )
}

type MobileDataRowProps = {
  label: string
  value: ReactNode
  className?: string
}

export function MobileDataRow({ label, value, className = '' }: MobileDataRowProps) {
  return (
    <div className={['grid grid-cols-1 gap-1 sm:grid-cols-[7rem_1fr] sm:gap-3', className].join(' ')}>
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</p>
      <div className="text-sm text-slate-900">{value}</div>
    </div>
  )
}

type MobileDataListProps = {
  children: ReactNode
  className?: string
}

export function MobileDataList({ children, className = '' }: MobileDataListProps) {
  return <div className={['space-y-3', className].join(' ')}>{children}</div>
}
