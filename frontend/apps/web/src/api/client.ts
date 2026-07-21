import type {
  ApiProblem,
  AuthenticatedUserResponse,
  ChangePasswordRequest,
  CsrfTokenResponse,
  InitializeAdminResponse,
  LoginRequest,
  SetupStatusResponse,
} from './contracts'

export interface ApiResult<T> {
  data?: T
  error?: ApiProblem
}

let csrfToken: string | undefined

async function request<TResponse, TBody = never>(
  path: string,
  init: Omit<RequestInit, 'body'> & { body?: TBody } = {},
): Promise<ApiResult<TResponse>> {
  const { body, ...fetchInit } = init
  const headers = new Headers(fetchInit.headers)
  const requestInit: RequestInit = {
    ...fetchInit,
    credentials: 'include',
    headers,
  }
  if (body !== undefined) {
    headers.set('Content-Type', 'application/json')
    requestInit.body = JSON.stringify(body)
  }

  const response = await fetch(path, requestInit)

  if (response.status === 204) {
    return {}
  }

  const responseBody = (await response.json().catch(() => undefined)) as
    | TResponse
    | ApiProblem
    | undefined

  if (!response.ok) {
    return { error: (responseBody as ApiProblem | undefined) ?? {} }
  }

  return { data: responseBody as TResponse }
}

export function clearCsrfToken(): void {
  csrfToken = undefined
}

export async function getCsrfToken(): Promise<string> {
  if (csrfToken) {
    return csrfToken
  }

  const response = await request<CsrfTokenResponse>('/api/v1/auth/csrf-token')
  if (!response.data) {
    throw new Error('無法建立安全請求，請稍後再試。')
  }

  csrfToken = response.data.token
  return csrfToken
}

export async function secureHeaders(): Promise<Record<string, string>> {
  return { 'X-XSRF-TOKEN': await getCsrfToken() }
}

export const apiClient = {
  getSetupStatus(): Promise<ApiResult<SetupStatusResponse>> {
    return request('/api/v1/setup/status')
  },

  initializeAdmin(headers: HeadersInit): Promise<ApiResult<InitializeAdminResponse>> {
    return request('/api/v1/setup/initialize', { method: 'POST', headers })
  },

  getCurrentUser(): Promise<ApiResult<AuthenticatedUserResponse>> {
    return request('/api/v1/auth/me')
  },

  refreshSession(headers: HeadersInit): Promise<ApiResult<AuthenticatedUserResponse>> {
    return request('/api/v1/auth/refresh', { method: 'POST', headers })
  },

  login(
    body: LoginRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<AuthenticatedUserResponse>> {
    return request('/api/v1/auth/login', { method: 'POST', headers, body })
  },

  changePassword(body: ChangePasswordRequest, headers: HeadersInit): Promise<ApiResult<void>> {
    return request('/api/v1/auth/password', { method: 'POST', headers, body })
  },

  logout(headers: HeadersInit): Promise<ApiResult<void>> {
    return request('/api/v1/auth/logout', { method: 'POST', headers })
  },
}

export function problemMessage(problem: unknown, fallback: string): string {
  if (problem && typeof problem === 'object') {
    const value = problem as ApiProblem
    const validationMessage = value.errors && Object.values(value.errors)[0]?.[0]
    return validationMessage ?? value.detail ?? value.title ?? fallback
  }

  return fallback
}
