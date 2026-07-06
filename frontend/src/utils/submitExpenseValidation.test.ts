import { describe, expect, it } from 'vitest'
import {
  hasSubmitExpenseFormErrors,
  validateSubmitExpenseForm,
} from './submitExpenseValidation'

describe('validateSubmitExpenseForm', () => {
  it('returns no errors for valid input', () => {
    const errors = validateSubmitExpenseForm({
      vendorName: 'Acme Corp',
      invoiceNumber: 'INV-1001',
      amount: '250.50',
      category: 'Travel',
    })

    expect(errors).toEqual({})
    expect(hasSubmitExpenseFormErrors(errors)).toBe(false)
  })

  it('requires vendor name', () => {
    const errors = validateSubmitExpenseForm({
      vendorName: '   ',
      invoiceNumber: 'INV-1001',
      amount: '100',
      category: 'Travel',
    })

    expect(errors.vendorName).toBe('Vendor name is required.')
  })

  it('requires invoice number', () => {
    const errors = validateSubmitExpenseForm({
      vendorName: 'Acme Corp',
      invoiceNumber: '',
      amount: '100',
      category: 'Travel',
    })

    expect(errors.invoiceNumber).toBe('Invoice number is required.')
  })

  it('rejects zero, negative, and non-numeric amounts', () => {
    expect(
      validateSubmitExpenseForm({
        vendorName: 'Acme Corp',
        invoiceNumber: 'INV-1001',
        amount: '0',
        category: 'Travel',
      }).amount,
    ).toBe('Enter a valid amount greater than zero.')

    expect(
      validateSubmitExpenseForm({
        vendorName: 'Acme Corp',
        invoiceNumber: 'INV-1001',
        amount: '-5',
        category: 'Travel',
      }).amount,
    ).toBe('Enter a valid amount greater than zero.')

    expect(
      validateSubmitExpenseForm({
        vendorName: 'Acme Corp',
        invoiceNumber: 'INV-1001',
        amount: 'abc',
        category: 'Travel',
      }).amount,
    ).toBe('Enter a valid amount greater than zero.')
  })

  it('requires category selection', () => {
    const errors = validateSubmitExpenseForm({
      vendorName: 'Acme Corp',
      invoiceNumber: 'INV-1001',
      amount: '100',
      category: '',
    })

    expect(errors.category).toBe('Select a category.')
  })

  it('returns all field errors when form is empty', () => {
    const errors = validateSubmitExpenseForm({
      vendorName: '',
      invoiceNumber: '',
      amount: '',
      category: '',
    })

    expect(errors.vendorName).toBeDefined()
    expect(errors.invoiceNumber).toBeDefined()
    expect(errors.amount).toBeDefined()
    expect(errors.category).toBeDefined()
    expect(hasSubmitExpenseFormErrors(errors)).toBe(true)
  })
})
