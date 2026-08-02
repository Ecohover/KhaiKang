import type {
  ApiProblem,
  AccountResponse,
  AddProjectMemberRequest,
  AuthenticatedUserResponse,
  ChangePasswordRequest,
  CreateAccountRequest,
  CreateAccountResponse,
  CreateProjectRequest,
  CreateIssueRequest,
  CreateTestCaseRequest,
  UpdateTestCaseRequest,
  CreateTestSuiteRequest,
  CreateTestWorkspaceRequest,
  CsrfTokenResponse,
  InitializeAdminResponse,
  IssueMetadataResponse,
  IssueResponse,
  LoginRequest,
  PagedResult,
  ProjectResponse,
  ProjectMemberResponse,
  ProjectRoleResponse,
  SetupStatusResponse,
  TestSuiteResponse,
  TestCaseResponse,
  TestWorkspaceMemberResponse,
  TestWorkspaceResponse,
  AddTestWorkspaceMemberRequest,
  UpdateProjectRequest,
  UpdateProjectMemberRolesRequest,
  UpdateAccountStatusRequest,
  UpdateAccountRequest,
  UpdateIssueStatusRequest,
  UpdateIssueAssigneeRequest,
  UpdateIssueRequest,
  UpdateTestSuiteRequest,
  UpdateTestWorkspaceMemberRequest,
  UpdateTestWorkspaceRequest,
  CreateTestPlanRequest,
  UpdateTestPlanRequest,
  TestPlanResponse,
  CreateTestRunRequest,
  UpdateTestRunStatusRequest,
  RecordTestResultRequest,
  TestRunResponse,
} from './contracts'
import { i18n } from '../i18n/index'

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
    throw new Error(i18n.global.t('system.errors.secureRequest'))
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

  listAccounts(): Promise<ApiResult<AccountResponse[]>> {
    return request('/api/v1/accounts')
  },

  createAccount(
    body: CreateAccountRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<CreateAccountResponse>> {
    return request('/api/v1/accounts', { method: 'POST', headers, body })
  },

  updateAccountStatus(
    accountId: string,
    body: UpdateAccountStatusRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<AccountResponse>> {
    return request(`/api/v1/accounts/${accountId}/status`, {
      method: 'PUT',
      headers,
      body,
    })
  },

  updateAccount(
    accountId: string,
    body: UpdateAccountRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<AccountResponse>> {
    return request(`/api/v1/accounts/${accountId}`, {
      method: 'PUT',
      headers,
      body,
    })
  },

  listProjects(): Promise<ApiResult<ProjectResponse[]>> {
    return request('/api/v1/projects')
  },

  getProject(projectId: string): Promise<ApiResult<ProjectResponse>> {
    return request(`/api/v1/projects/${projectId}`)
  },

  createProject(
    body: CreateProjectRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<ProjectResponse>> {
    return request('/api/v1/projects', { method: 'POST', headers, body })
  },

  updateProject(
    projectId: string,
    body: UpdateProjectRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<ProjectResponse>> {
    return request(`/api/v1/projects/${projectId}`, { method: 'PUT', headers, body })
  },

  listProjectRoles(projectId: string): Promise<ApiResult<ProjectRoleResponse[]>> {
    return request(`/api/v1/projects/${projectId}/roles`)
  },

  listProjectMembers(projectId: string): Promise<ApiResult<ProjectMemberResponse[]>> {
    return request(`/api/v1/projects/${projectId}/members`)
  },

  addProjectMember(
    projectId: string,
    body: AddProjectMemberRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<ProjectMemberResponse>> {
    return request(`/api/v1/projects/${projectId}/members`, { method: 'POST', headers, body })
  },

  updateProjectMemberRoles(
    projectId: string,
    memberId: string,
    body: UpdateProjectMemberRolesRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<ProjectMemberResponse>> {
    return request(`/api/v1/projects/${projectId}/members/${memberId}/roles`, {
      method: 'PUT',
      headers,
      body,
    })
  },

  removeProjectMember(
    projectId: string,
    memberId: string,
    version: number,
    headers: HeadersInit,
  ): Promise<ApiResult<void>> {
    return request(
      `/api/v1/projects/${projectId}/members/${memberId}?version=${encodeURIComponent(version)}`,
      { method: 'DELETE', headers },
    )
  },

  listIssues(
    projectId: string,
    page = 1,
    pageSize = 20,
  ): Promise<ApiResult<PagedResult<IssueResponse>>> {
    const query = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    })
    return request(`/api/v1/projects/${projectId}/issues?${query}`)
  },

  getIssueMetadata(projectId: string): Promise<ApiResult<IssueMetadataResponse>> {
    return request(`/api/v1/projects/${projectId}/issues/metadata`)
  },

  getIssue(projectId: string, issueId: string): Promise<ApiResult<IssueResponse>> {
    return request(`/api/v1/projects/${projectId}/issues/${issueId}`)
  },

  createIssue(
    projectId: string,
    body: CreateIssueRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<IssueResponse>> {
    return request(`/api/v1/projects/${projectId}/issues`, { method: 'POST', headers, body })
  },

  updateIssueStatus(
    projectId: string,
    issueId: string,
    body: UpdateIssueStatusRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<IssueResponse>> {
    return request(`/api/v1/projects/${projectId}/issues/${issueId}/status`, {
      method: 'PUT',
      headers,
      body,
    })
  },

  updateIssueAssignee(
    projectId: string,
    issueId: string,
    body: UpdateIssueAssigneeRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<IssueResponse>> {
    return request(`/api/v1/projects/${projectId}/issues/${issueId}/assignee`, {
      method: 'PUT',
      headers,
      body,
    })
  },

  updateIssue(
    projectId: string,
    issueId: string,
    body: UpdateIssueRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<IssueResponse>> {
    return request(`/api/v1/projects/${projectId}/issues/${issueId}`, {
      method: 'PUT',
      headers,
      body,
    })
  },

  listTestWorkspaces(): Promise<ApiResult<TestWorkspaceResponse[]>> {
    return request('/api/v1/test-workspaces')
  },

  getTestWorkspace(workspaceId: string): Promise<ApiResult<TestWorkspaceResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}`)
  },

  createTestWorkspace(
    body: CreateTestWorkspaceRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestWorkspaceResponse>> {
    return request('/api/v1/test-workspaces', { method: 'POST', headers, body })
  },

  updateTestWorkspace(
    workspaceId: string,
    body: UpdateTestWorkspaceRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestWorkspaceResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}`, { method: 'PUT', headers, body })
  },

  listTestWorkspaceMembers(
    workspaceId: string,
  ): Promise<ApiResult<TestWorkspaceMemberResponse[]>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/members`)
  },

  addTestWorkspaceMember(
    workspaceId: string,
    body: AddTestWorkspaceMemberRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestWorkspaceMemberResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/members`, {
      method: 'POST', headers, body,
    })
  },

  updateTestWorkspaceMember(
    workspaceId: string,
    memberId: string,
    body: UpdateTestWorkspaceMemberRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestWorkspaceMemberResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/members/${memberId}`, {
      method: 'PUT', headers, body,
    })
  },

  removeTestWorkspaceMember(
    workspaceId: string,
    memberId: string,
    version: number,
    headers: HeadersInit,
  ): Promise<ApiResult<void>> {
    return request(
      `/api/v1/test-workspaces/${workspaceId}/members/${memberId}?version=${version}`,
      { method: 'DELETE', headers },
    )
  },

  listTestSuites(workspaceId: string): Promise<ApiResult<TestSuiteResponse[]>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/suites`)
  },

  createTestSuite(
    workspaceId: string,
    body: CreateTestSuiteRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestSuiteResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/suites`, {
      method: 'POST', headers, body,
    })
  },

  updateTestSuite(
    workspaceId: string,
    suiteId: string,
    body: UpdateTestSuiteRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestSuiteResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/suites/${suiteId}`, {
      method: 'PUT', headers, body,
    })
  },

  listTestCases(
    workspaceId: string,
    suiteId?: string,
  ): Promise<ApiResult<TestCaseResponse[]>> {
    const query = suiteId ? `?suiteId=${encodeURIComponent(suiteId)}` : ''
    return request(`/api/v1/test-workspaces/${workspaceId}/cases${query}`)
  },

  createTestCase(
    workspaceId: string,
    body: CreateTestCaseRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestCaseResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/cases`, {
      method: 'POST', headers, body,
    })
  },

  getTestCase(
    workspaceId: string,
    caseId: string,
  ): Promise<ApiResult<TestCaseResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/cases/${caseId}`)
  },

  updateTestCase(
    workspaceId: string,
    caseId: string,
    body: UpdateTestCaseRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestCaseResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/cases/${caseId}`, {
      method: 'PUT', headers, body,
    })
  },

  listTestPlans(workspaceId: string): Promise<ApiResult<TestPlanResponse[]>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/plans`)
  },

  getTestPlan(workspaceId: string, planId: string): Promise<ApiResult<TestPlanResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/plans/${planId}`)
  },

  createTestPlan(
    workspaceId: string,
    body: CreateTestPlanRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestPlanResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/plans`, {
      method: 'POST', headers, body,
    })
  },

  updateTestPlan(
    workspaceId: string,
    planId: string,
    body: UpdateTestPlanRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestPlanResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/plans/${planId}`, {
      method: 'PUT', headers, body,
    })
  },

  listTestRuns(workspaceId: string): Promise<ApiResult<TestRunResponse[]>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/runs`)
  },

  getTestRun(workspaceId: string, runId: string): Promise<ApiResult<TestRunResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/runs/${runId}`)
  },

  createTestRun(
    workspaceId: string,
    body: CreateTestRunRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestRunResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/runs`, {
      method: 'POST', headers, body,
    })
  },

  recordTestRunItem(
    workspaceId: string,
    runId: string,
    itemId: string,
    body: RecordTestResultRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestRunResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/runs/${runId}/items/${itemId}`, {
      method: 'PUT', headers, body,
    })
  },

  recordTestRunStep(
    workspaceId: string,
    runId: string,
    itemId: string,
    stepId: string,
    body: RecordTestResultRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestRunResponse>> {
    return request(
      `/api/v1/test-workspaces/${workspaceId}/runs/${runId}/items/${itemId}/steps/${stepId}`,
      { method: 'PUT', headers, body },
    )
  },

  updateTestRunStatus(
    workspaceId: string,
    runId: string,
    body: UpdateTestRunStatusRequest,
    headers: HeadersInit,
  ): Promise<ApiResult<TestRunResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/runs/${runId}/status`, {
      method: 'PUT', headers, body,
    })
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
