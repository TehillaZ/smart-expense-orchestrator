import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { EscalationDecisionDialog } from './EscalationDecisionDialog'

const sampleItem = {
  trackingId: 'track-1',
  vendor: 'Acme Corp',
  invoiceNumber: 'INV-1003',
  amount: 500,
  category: 'Meals',
  aiRecommendation: 'Escalate',
  confidence: 0.72,
  citedRules: ['AUTONOMY-CEILING'],
  reason: 'Escalated for human review.',
}

afterEach(() => {
  cleanup()
})

describe('EscalationDecisionDialog', () => {
  it('requires a comment before confirming RequestInfo', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()

    render(
      <EscalationDecisionDialog
        item={sampleItem}
        decision="RequestInfo"
        isSubmitting={false}
        onConfirm={onConfirm}
        onCancel={() => undefined}
      />,
    )

    const confirmButton = screen.getByRole('button', { name: 'Confirm' })
    expect(confirmButton).toBeDisabled()

    await user.type(
      screen.getByLabelText('Question for submitter (required)'),
      'Please provide the itemized receipt.',
    )

    expect(confirmButton).toBeEnabled()
    await user.click(confirmButton)

    expect(onConfirm).toHaveBeenCalledWith('Please provide the itemized receipt.')
  })

  it('allows Approve without a comment', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()

    render(
      <EscalationDecisionDialog
        item={sampleItem}
        decision="Approve"
        isSubmitting={false}
        onConfirm={onConfirm}
        onCancel={() => undefined}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Confirm' }))

    expect(onConfirm).toHaveBeenCalledWith('')
  })
})
