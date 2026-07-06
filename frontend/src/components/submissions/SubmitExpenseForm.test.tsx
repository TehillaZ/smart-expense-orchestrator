import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { SubmitExpenseForm } from './SubmitExpenseForm'

afterEach(() => {
  cleanup()
})

describe('SubmitExpenseForm', () => {
  it('shows validation errors when submitted empty', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()

    render(<SubmitExpenseForm onSubmit={onSubmit} />)

    await user.click(screen.getByRole('button', { name: 'Submit Expense' }))

    expect(screen.getByText('Vendor name is required.')).toBeInTheDocument()
    expect(screen.getByText('Invoice number is required.')).toBeInTheDocument()
    expect(screen.getByText('Enter a valid amount greater than zero.')).toBeInTheDocument()
    expect(screen.getByText('Select a category.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits trimmed values when the form is valid', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)

    render(<SubmitExpenseForm onSubmit={onSubmit} />)

    await user.type(screen.getByLabelText('Vendor Name'), '  Acme Corp  ')
    await user.type(screen.getByLabelText('Invoice Number'), ' INV-2001 ')
    await user.type(screen.getByLabelText('Amount ($)'), '450')
    await user.selectOptions(screen.getByLabelText('Category'), 'Software')
    await user.click(screen.getByRole('button', { name: 'Submit Expense' }))

    expect(onSubmit).toHaveBeenCalledWith({
      vendorName: 'Acme Corp',
      invoiceNumber: 'INV-2001',
      amount: 450,
      category: 'Software',
    })
  })
})
