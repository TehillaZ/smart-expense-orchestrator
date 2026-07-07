import { describe, expect, it } from 'vitest'
import { hasDuplicateResubmits } from '../services/submissionService'
import type { SubmissionStatusResponse } from '../types/api'

function buildSubmission(
  overrides: Partial<SubmissionStatusResponse> = {},
): SubmissionStatusResponse {
  return {
    trackingId: 'tracking-1',
    correlationId: 'correlation-1',
    vendor: 'Acme Corp',
    invoiceNumber: 'INV-1001',
    amount: 100,
    category: 'Travel',
    status: 'Received',
    reason: 'Invoice received and is pending analysis.',
    ...overrides,
  }
}

describe('hasDuplicateResubmits', () => {
  it('returns false when duplicateResubmitCount is missing or zero', () => {
    expect(hasDuplicateResubmits(buildSubmission())).toBe(false)
    expect(hasDuplicateResubmits(buildSubmission({ duplicateResubmitCount: 0 }))).toBe(false)
  })

  it('returns true when duplicateResubmitCount is greater than zero', () => {
    expect(hasDuplicateResubmits(buildSubmission({ duplicateResubmitCount: 1 }))).toBe(true)
    expect(hasDuplicateResubmits(buildSubmission({ duplicateResubmitCount: 3 }))).toBe(true)
  })
})
