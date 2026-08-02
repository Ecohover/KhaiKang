import { describe, expect, it } from 'vitest'
import {
  ACCOUNT_READ_PERMISSION,
  hasRequiredSystemPermission,
  visibleApplicationModules,
} from './navigation'

describe('navigation permissions', () => {
  it('shows ordinary workspace navigation to an authenticated user', () => {
    expect(visibleApplicationModules([]).map((item) => item.id)).toEqual(['projects', 'tests'])
  })

  it('shows administration navigation to a system administrator', () => {
    expect(
      visibleApplicationModules([ACCOUNT_READ_PERMISSION]).map((item) => item.id),
    ).toContain('system')
  })

  it('rejects a route when none of the required roles are present', () => {
    expect(hasRequiredSystemPermission([], [ACCOUNT_READ_PERMISSION])).toBe(false)
  })
})
