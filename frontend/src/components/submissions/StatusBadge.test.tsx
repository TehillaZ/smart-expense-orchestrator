import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { SUBMISSION_STATUSES } from '../../types/submission'
import { StatusBadge } from './StatusBadge'

describe('StatusBadge', () => {
  it.each(SUBMISSION_STATUSES)('renders %s label and status styling', (status) => {
    render(<StatusBadge status={status} />)

    const badge = screen.getByText(status)
    expect(badge).toBeInTheDocument()
    expect(badge.className).toContain('rounded-full')
  })
})
