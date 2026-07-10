import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import type { SubmissionDetail } from '../../types/submission'
import { SubmissionDetailPanel } from './SubmissionDetailPanel'

afterEach(() => {
  cleanup()
})

function renderPanel(detail: SubmissionDetail) {
  return render(
    <MemoryRouter>
      <Routes>
        <Route element={<Outlet context={{ role: 'Submitter' }} />}>
          <Route path="/" element={<SubmissionDetailPanel detail={detail} onClose={() => undefined} />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

function baseDetail(overrides: Partial<SubmissionDetail> = {}): SubmissionDetail {
  return {
    trackingId: 'trk-demo-1',
    correlationId: 'corr-demo-1',
    vendor: 'Acme',
    amount: 900,
    status: 'Approved',
    invoiceNumber: 'RS-90021',
    category: 'Travel',
    backendStatus: 'Compensated',
    reason: 'Payment failed; reservation released during compensation.',
    ...overrides,
  }
}

describe('SubmissionDetailPanel compensation banner', () => {
  it('shows payment failed then OK money-back for Compensated status', () => {
    renderPanel(baseDetail())

    const banner = screen.getByTestId('compensation-outcome-banner')
    expect(banner).toBeInTheDocument()
    expect(banner).toHaveTextContent('Payment failed')
    expect(banner).toHaveTextContent(
      'Payment failed; reservation released during compensation.',
    )

    const moneyBack = screen.getByTestId('money-back-message')
    expect(moneyBack).toHaveTextContent('OK — you get your money back')
    expect(moneyBack).toHaveTextContent('you get your money back')
    expect(screen.getByText('Compensated', { selector: '.bg-amber-100' })).toBeInTheDocument()
  })

  it('shows a payment-failed banner without the money-back message', () => {
    renderPanel(
      baseDetail({
        backendStatus: 'PaymentFailed',
        reason: 'Charge declined by gateway.',
      }),
    )

    const banner = screen.getByTestId('compensation-outcome-banner')
    expect(banner).toBeInTheDocument()
    expect(banner).toHaveTextContent('Payment failed')
    expect(banner).toHaveTextContent('Charge declined by gateway.')
    expect(screen.queryByTestId('money-back-message')).not.toBeInTheDocument()
  })

  it('shows a payment-in-progress note for PaymentPending', () => {
    renderPanel(baseDetail({ backendStatus: 'PaymentPending', reason: 'Awaiting payment.' }))

    expect(screen.getByText('Payment in progress…')).toBeInTheDocument()
    expect(screen.queryByTestId('compensation-outcome-banner')).not.toBeInTheDocument()
  })
})
