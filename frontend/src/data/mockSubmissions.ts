import type { SubmissionRecord } from '../types/submission'

export const MOCK_SUBMISSIONS: SubmissionRecord[] = [
  {
    trackingId: 'TRK-8F2A91',
    vendor: 'CloudHost Inc.',
    amount: 249.0,
    status: 'Approved',
    invoiceNumber: 'INV-1001',
    category: 'Software',
  },
  {
    trackingId: 'TRK-3C7B44',
    vendor: 'Metro Travel Co.',
    amount: 1180.5,
    status: 'Pending',
    invoiceNumber: 'INV-1003',
    category: 'Travel',
  },
  {
    trackingId: 'TRK-1D9E02',
    vendor: 'Office Depot',
    amount: 86.25,
    status: 'Rejected',
    invoiceNumber: 'INV-1008',
    category: 'Office Supplies',
  },
  {
    trackingId: 'TRK-5A6F33',
    vendor: 'Fresh Bites Catering',
    amount: 142.0,
    status: 'Approved',
    invoiceNumber: 'INV-1010',
    category: 'Meals',
  },
]
