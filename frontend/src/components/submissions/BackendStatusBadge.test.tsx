import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { BackendStatusBadge } from './BackendStatusBadge'

describe('BackendStatusBadge', () => {
  it('renders a human-readable label for camelCase backend statuses', () => {
    render(<BackendStatusBadge status="HumanApproved" />)

    expect(screen.getByText('Human Approved')).toBeInTheDocument()
  })

  it('applies known status styling classes', () => {
    render(<BackendStatusBadge status="Escalated" />)

    const badge = screen.getByText('Escalated')
    expect(badge.className).toContain('bg-violet-100')
  })

  it('styles Compensated as amber warning', () => {
    render(<BackendStatusBadge status="Compensated" />)

    const badge = screen.getByText('Compensated')
    expect(badge.className).toContain('bg-amber-100')
  })

  it('styles PaymentFailed as rose danger', () => {
    render(<BackendStatusBadge status="PaymentFailed" />)

    const badge = screen.getByText('Payment Failed')
    expect(badge.className).toContain('bg-rose-100')
  })

  it('falls back to default styling for unknown statuses', () => {
    render(<BackendStatusBadge status="CustomStatus" />)

    const badge = screen.getByText('Custom Status')
    expect(badge.className).toContain('bg-slate-100')
  })
})
