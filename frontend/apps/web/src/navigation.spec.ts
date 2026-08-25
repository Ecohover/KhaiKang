import { describe, expect, it } from 'vitest'
import {
  ACCOUNT_READ_PERMISSION,
  hasRequiredSystemPermission,
  visibleApplicationModules,
} from './navigation'

describe('navigation permissions', () => {
  it('shows owner-accessible AI accounts and ordinary workspaces to an authenticated user', () => {
    expect(visibleApplicationModules([]).map((item) => item.id)).toEqual(['aiAccounts', 'projects', 'tests'])
  })

  it('shows administration navigation to a system administrator', () => {
    expect(
      visibleApplicationModules([ACCOUNT_READ_PERMISSION]).map((item) => item.id),
    ).toContain('system')
  })

  it('does not show human AI-account management to an AI Web identity', () => {
    expect(visibleApplicationModules([], 'ai_agent').map((item) => item.id)).not.toContain('aiAccounts')
  })

  it('rejects a route when none of the required roles are present', () => {
    expect(hasRequiredSystemPermission([], [ACCOUNT_READ_PERMISSION])).toBe(false)
  })
})
