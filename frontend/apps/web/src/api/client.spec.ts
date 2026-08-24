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

describe('AI account client', () => {
  it('creates an AI account through the human cookie and CSRF boundary', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ account: {}, initialPassword: 'one-time' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiClient.createAiAccount(
      { username: 'ai-development', fixedRole: 'development_ai' },
      { 'X-XSRF-TOKEN': 'csrf-token' },
    )

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/ai-accounts',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: JSON.stringify({ username: 'ai-development', fixedRole: 'development_ai' }),
      }),
    )
  })

  it('keeps token plaintext on the explicit owner command response only', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ token: 'kkai-secret', createdAt: '2026-08-24T00:00:00Z', version: 1 }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const response = await apiClient.createOrRecreateAiAccountToken(
      'ai-account-id',
      { 'X-XSRF-TOKEN': 'csrf-token' },
    )

    expect(response.data?.token).toBe('kkai-secret')
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/ai-accounts/ai-account-id/token',
      expect.objectContaining({ method: 'POST', credentials: 'include' }),
    )
  })

  it('uses an in-memory bearer argument without cookie token persistence', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ id: 'ai-id' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiClient.getCurrentAiBearerIdentity('kkai-secret')

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    const headers = init.headers as Headers
    expect(headers.get('Authorization')).toBe('Bearer kkai-secret')
    expect(init.credentials).toBe('omit')
  })
})

describe('comment client', () => {
  it('lists a stable comment page through the canonical resource route', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ items: [], page: 2, hasNextPage: false }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiClient.listComments('test_run', 'run-id', 2)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/comments/test_run/run-id?page=2',
      expect.objectContaining({ credentials: 'include' }),
    )
  })

  it('creates a comment with CSRF and the caller-owned idempotency key', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: 'comment-id',
          authorAccountId: 'account-id',
          authorDisplayName: 'developer',
          accountType: 'human',
          fixedRole: null,
          ownerAccountId: null,
          ownerDisplayName: null,
          content: 'Verification evidence.',
          createdAt: '2026-08-24T00:00:00Z',
          version: 1,
        }),
        { status: 201, headers: { 'Content-Type': 'application/json' } },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiClient.createComment(
      {
        resourceType: 'issue',
        resourceId: 'issue-id',
        idempotencyKey: 'comment-key',
        content: 'Verification evidence.',
      },
      { 'X-XSRF-TOKEN': 'csrf-token' },
    )

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    const headers = init.headers as Headers
    expect(headers.get('X-XSRF-TOKEN')).toBe('csrf-token')
    expect(headers.get('Idempotency-Key')).toBe('comment-key')
    expect(init.body).toBe(JSON.stringify({ content: 'Verification evidence.' }))
  })
})
