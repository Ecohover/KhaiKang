import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiClient, problemMessage } from './client'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('problemMessage', () => {
  it('returns the first validation message', () => {
    expect(
      problemMessage(
        { errors: { password: ['Password is too short.'] } },
        'Fallback',
      ),
    ).toBe('Password is too short.')
  })

  it('uses the fallback for an unknown error shape', () => {
    expect(problemMessage(null, 'Fallback')).toBe('Fallback')
  })
})

describe('account management client', () => {
  it('updates an account through the account resource endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: 'account-id',
          username: 'reviewer.two',
          accountType: 'human',
          status: 'active',
          systemRoles: ['User'],
          mustChangePassword: true,
          lastLoginAt: null,
          createdAt: '2026-07-24T00:00:00Z',
          updatedAt: '2026-07-24T00:00:00Z',
          version: 2,
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiClient.updateAccount(
      'account-id',
      { username: 'reviewer.two', version: 1 },
      { 'X-XSRF-TOKEN': 'token' },
    )

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/accounts/account-id',
      expect.objectContaining({
        method: 'PUT',
        credentials: 'include',
        body: JSON.stringify({ username: 'reviewer.two', version: 1 }),
      }),
    )
  })

  it('uses the dedicated account status endpoint with the concurrency version', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: 'account-id',
          username: 'reviewer',
          accountType: 'human',
          status: 'suspended',
          systemRoles: ['User'],
          mustChangePassword: true,
          lastLoginAt: null,
          createdAt: '2026-07-24T00:00:00Z',
          updatedAt: '2026-07-24T00:00:00Z',
          version: 2,
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiClient.updateAccountStatus(
      'account-id',
      { status: 'suspended', version: 1 },
      { 'X-XSRF-TOKEN': 'token' },
    )

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/accounts/account-id/status',
      expect.objectContaining({
        method: 'PUT',
        credentials: 'include',
        body: JSON.stringify({ status: 'suspended', version: 1 }),
      }),
    )
  })
})

describe('issue management client', () => {
  it('sends the standard one-based paging parameters', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          items: [],
          page: 3,
          pageSize: 50,
          totalCount: 0,
          totalPages: 0,
          hasPreviousPage: true,
          hasNextPage: false,
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiClient.listIssues('project-id', 3, 50)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/projects/project-id/issues?page=3&pageSize=50',
      expect.objectContaining({
        credentials: 'include',
      }),
    )
  })
})
