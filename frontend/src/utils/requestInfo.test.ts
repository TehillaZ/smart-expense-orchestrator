import { describe, expect, it } from 'vitest'
import {
  getInfoRequestMessage,
  isInfoRequestedQueueItem,
  isInfoRequestedSubmission,
} from './requestInfo'

describe('requestInfo helpers', () => {
  it('detects RequestInfo on submissions and queue items', () => {
    expect(isInfoRequestedSubmission({ humanDecision: 'RequestInfo' })).toBe(true)
    expect(isInfoRequestedQueueItem({ humanDecision: 'RequestInfo' } as never)).toBe(true)
    expect(isInfoRequestedSubmission({ humanDecision: 'Approve' })).toBe(false)
  })

  it('prefers humanDecisionComment for the info message', () => {
    expect(
      getInfoRequestMessage({
        humanDecision: 'RequestInfo',
        humanDecisionComment: 'Please upload the itemized receipt.',
        reason: 'More information was requested before a final decision.',
      }),
    ).toBe('Please upload the itemized receipt.')
  })
})
