import { describe, expect, it } from 'vitest'
import { canAccessPath, getDefaultRoute, getNavItemsForRole } from '../types/userRole'

describe('userRole', () => {
  it('maps Admin to dashboard, audit, and policy navigation', () => {
    const items = getNavItemsForRole('Admin')
    const labels = items.map((item) => item.label)

    expect(labels).toEqual(['Dashboard', 'Audit Trail', 'Policy'])
  })

  it('allows submitter routes only for Submitter', () => {
    expect(canAccessPath('Submitter', '/submissions')).toBe(true)
    expect(canAccessPath('Submitter', '/track')).toBe(true)
    expect(canAccessPath('Submitter', '/approvals')).toBe(false)
    expect(canAccessPath('Submitter', '/policy')).toBe(false)
  })

  it('allows approval routes only for Approver', () => {
    expect(canAccessPath('Approver', '/approvals')).toBe(true)
    expect(canAccessPath('Approver', '/approvals/abc-123')).toBe(true)
    expect(canAccessPath('Approver', '/submissions')).toBe(false)
  })

  it('returns role-specific default routes', () => {
    expect(getDefaultRoute('Submitter')).toBe('/submissions')
    expect(getDefaultRoute('Approver')).toBe('/approvals')
    expect(getDefaultRoute('Admin')).toBe('/dashboard')
  })
})
