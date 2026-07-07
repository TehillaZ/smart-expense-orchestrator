import type { EscalationQueueItem } from '../types/api'
import { isInfoRequestedQueueItem } from './requestInfo'

export type EscalationQueueFilters = {
  category: string
  minAmount: string
  maxAmount: string
  infoRequestedOnly: boolean
  searchText: string
}

export const EMPTY_ESCALATION_QUEUE_FILTERS: EscalationQueueFilters = {
  category: '',
  minAmount: '',
  maxAmount: '',
  infoRequestedOnly: false,
  searchText: '',
}

export function filterEscalationQueue(
  items: EscalationQueueItem[],
  filters: EscalationQueueFilters,
): EscalationQueueItem[] {
  const search = filters.searchText.trim().toLowerCase()
  const minAmount = filters.minAmount.trim() ? Number.parseFloat(filters.minAmount) : null
  const maxAmount = filters.maxAmount.trim() ? Number.parseFloat(filters.maxAmount) : null

  return items.filter((item) => {
    if (filters.category && item.category !== filters.category) {
      return false
    }

    if (minAmount != null && !Number.isNaN(minAmount) && item.amount < minAmount) {
      return false
    }

    if (maxAmount != null && !Number.isNaN(maxAmount) && item.amount > maxAmount) {
      return false
    }

    if (filters.infoRequestedOnly && !isInfoRequestedQueueItem(item)) {
      return false
    }

    if (search) {
      const haystack = `${item.vendor} ${item.invoiceNumber} ${item.trackingId}`.toLowerCase()
      if (!haystack.includes(search)) {
        return false
      }
    }

    return true
  })
}

export function hasActiveEscalationQueueFilters(filters: EscalationQueueFilters): boolean {
  return (
    filters.category !== '' ||
    filters.minAmount.trim() !== '' ||
    filters.maxAmount.trim() !== '' ||
    filters.infoRequestedOnly ||
    filters.searchText.trim() !== ''
  )
}
