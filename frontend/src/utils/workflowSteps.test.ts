import { describe, expect, it } from 'vitest'
import {
  getWorkflowSteps,
  isAuditFailureLabel,
  isPaymentCompensationStatus,
} from './workflowSteps'

describe('getWorkflowSteps', () => {
  it('marks payment as complete for Paid', () => {
    const steps = getWorkflowSteps('Paid')
    const payment = steps.find((step) => step.id === 'payment')

    expect(payment).toEqual({ id: 'payment', label: 'Paid', state: 'complete' })
    expect(steps.slice(0, 3).every((step) => step.state === 'complete')).toBe(true)
  })

  it('marks payment as failed for PaymentFailed', () => {
    const steps = getWorkflowSteps('PaymentFailed')
    const payment = steps.find((step) => step.id === 'payment')

    expect(payment).toEqual({
      id: 'payment',
      label: 'Payment failed',
      state: 'failed',
    })
  })

  it('marks payment as compensated for Compensated', () => {
    const steps = getWorkflowSteps('Compensated')
    const payment = steps.find((step) => step.id === 'payment')

    expect(payment).toEqual({
      id: 'payment',
      label: 'Compensated',
      state: 'compensated',
    })
  })

  it('keeps payment as current while PaymentPending', () => {
    const steps = getWorkflowSteps('PaymentPending')
    const payment = steps.find((step) => step.id === 'payment')

    expect(payment).toEqual({ id: 'payment', label: 'Payment', state: 'current' })
  })
})

describe('isPaymentCompensationStatus', () => {
  it('detects compensated and payment-failed statuses', () => {
    expect(isPaymentCompensationStatus('Compensated')).toBe(true)
    expect(isPaymentCompensationStatus('PaymentFailed')).toBe(true)
    expect(isPaymentCompensationStatus('Paid')).toBe(false)
  })
})

describe('isAuditFailureLabel', () => {
  it('detects failure and compensation labels', () => {
    expect(isAuditFailureLabel('PaymentFailed')).toBe(true)
    expect(isAuditFailureLabel('Compensated')).toBe(true)
    expect(isAuditFailureLabel('payment.failed')).toBe(true)
    expect(isAuditFailureLabel('Reservation released after compensation')).toBe(true)
    expect(isAuditFailureLabel('Paid')).toBe(false)
  })
})
