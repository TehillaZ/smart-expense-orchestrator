import { describe, expect, it } from 'vitest'
import type { EscalationQueueItem } from '../types/api'
import {
  EMPTY_ESCALATION_QUEUE_FILTERS,
  filterEscalationQueue,
  hasActiveEscalationQueueFilters,
} from './escalationQueueFilters'

const sampleItems: EscalationQueueItem[] = [
  {
    trackingId: 't-1',
    vendor: 'Acme Corp',
    invoiceNumber: 'INV-1001',
    amount: 500,
    category: 'Travel',
    aiRecommendation: 'Escalate',
    confidence: 0.7,
    citedRules: ['AUTONOMY-CEILING'],
    reason: 'Amount exceeds ceiling.',
  },
  {
    trackingId: 't-2',
    vendor: 'Beta LLC',
    invoiceNumber: 'INV-1002',
    amount: 120,
    category: 'Meals',
    aiRecommendation: 'Escalate',
    confidence: 0.6,
    citedRules: ['LOW-CONFIDENCE'],
    reason: 'Confidence below threshold.',
    humanDecision: 'RequestInfo',
    humanDecisionComment: 'Need receipt',
  },
]

describe('filterEscalationQueue', () => {
  it('returns all items when filters are empty', () => {
    expect(filterEscalationQueue(sampleItems, EMPTY_ESCALATION_QUEUE_FILTERS)).toHaveLength(2)
  })

  it('filters by category and amount range', () => {
    const filtered = filterEscalationQueue(sampleItems, {
      ...EMPTY_ESCALATION_QUEUE_FILTERS,
      category: 'Travel',
      minAmount: '400',
    })

    expect(filtered).toHaveLength(1)
    expect(filtered[0]?.trackingId).toBe('t-1')
  })

  it('filters info-requested items only', () => {
    const filtered = filterEscalationQueue(sampleItems, {
      ...EMPTY_ESCALATION_QUEUE_FILTERS,
      infoRequestedOnly: true,
    })

    expect(filtered).toHaveLength(1)
    expect(filtered[0]?.trackingId).toBe('t-2')
  })
})

describe('hasActiveEscalationQueueFilters', () => {
  it('detects active filters', () => {
    expect(hasActiveEscalationQueueFilters(EMPTY_ESCALATION_QUEUE_FILTERS)).toBe(false)
    expect(
      hasActiveEscalationQueueFilters({
        ...EMPTY_ESCALATION_QUEUE_FILTERS,
        searchText: 'acme',
      }),
    ).toBe(true)
  })
})
