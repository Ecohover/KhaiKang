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
  IssueListQuery,
  IssueResponse,
  IssueAttachmentResponse,
  LoginRequest,
  PagedResult,
  ProjectResponse,
  ProjectMemberResponse,
  ProjectRoleResponse,
  SetupStatusResponse,
  TestSuiteResponse,
  TestCaseResponse,
  TestCaseAttachmentResponse,
  TestRunItemAttachmentResponse,
  TestTagResponse,
  CreateTestTagRequest,
  UpdateTestTagRequest,
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

async function requestForm<TResponse>(
  path: string,
  formData: FormData,
  init: Omit<RequestInit, 'body'> = {},
): Promise<ApiResult<TResponse>> {
  const response = await fetch(path, {
    ...init,
    body: formData,
    credentials: 'include',
  })
  const responseBody = (await response.json().catch(() => undefined)) as
    | TResponse
    | ApiProblem
    | undefined
  if (!response.ok) return { error: (responseBody as ApiProblem | undefined) ?? {} }
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
    filters: IssueListQuery = {},
  ): Promise<ApiResult<PagedResult<IssueResponse>>> {
    const query = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    })
    if (filters.search) query.set('search', filters.search)
    if (filters.typeCode) query.set('typeCode', filters.typeCode)
    if (filters.statusCode) query.set('statusCode', filters.statusCode)
    if (filters.priorityCode) query.set('priorityCode', filters.priorityCode)
    if (filters.assigneeAccountId) query.set('assigneeAccountId', filters.assigneeAccountId)
    if (filters.unassigned) query.set('unassigned', 'true')
    if (filters.sortBy) query.set('sortBy', filters.sortBy)
    if (filters.sortDirection) query.set('sortDirection', filters.sortDirection)
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

  listIssueAttachments(
    projectId: string,
    issueId: string,
  ): Promise<ApiResult<IssueAttachmentResponse[]>> {
    return request(`/api/v1/projects/${projectId}/issues/${issueId}/attachments`)
  },

  uploadIssueAttachment(
    projectId: string,
    issueId: string,
    file: File,
    headers: HeadersInit,
  ): Promise<ApiResult<IssueAttachmentResponse>> {
    const formData = new FormData()
    formData.append('file', file, file.name)
    return requestForm(`/api/v1/projects/${projectId}/issues/${issueId}/attachments`, formData, {
      method: 'POST',
      headers,
    })
  },

  deleteIssueAttachment(
    projectId: string,
    issueId: string,
    attachmentId: string,
    headers: HeadersInit,
  ): Promise<ApiResult<void>> {
    return request(`/api/v1/projects/${projectId}/issues/${issueId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      headers,
    })
  },

  issueAttachmentContentUrl(
    projectId: string,
    issueId: string,
    attachmentId: string,
    inline = false,
  ): string {
    const suffix = inline ? '?inline=true' : ''
    return `/api/v1/projects/${projectId}/issues/${issueId}/attachments/${attachmentId}/content${suffix}`
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
    filters: { suiteId?: string, search?: string, status?: 'active' | 'inactive', tagId?: string } = {},
  ): Promise<ApiResult<TestCaseResponse[]>> {
    const params = new URLSearchParams()
    if (filters.suiteId) params.set('suiteId', filters.suiteId)
    if (filters.search) params.set('search', filters.search)
    if (filters.status) params.set('status', filters.status)
    if (filters.tagId) params.set('tagId', filters.tagId)
    const query = params.size ? `?${params.toString()}` : ''
    return request(`/api/v1/test-workspaces/${workspaceId}/cases${query}`)
  },

  listTestTags(): Promise<ApiResult<TestTagResponse[]>> {
    return request('/api/v1/test-tags')
  },

  createTestTag(body: CreateTestTagRequest, headers: HeadersInit): Promise<ApiResult<TestTagResponse>> {
    return request('/api/v1/test-tags', { method: 'POST', headers, body })
  },

  updateTestTag(tagId: string, body: UpdateTestTagRequest, headers: HeadersInit): Promise<ApiResult<TestTagResponse>> {
    return request(`/api/v1/test-tags/${tagId}`, { method: 'PUT', headers, body })
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

  uploadTestCaseAttachment(
    workspaceId: string,
    caseId: string,
    file: File,
    headers: HeadersInit,
  ): Promise<ApiResult<TestCaseAttachmentResponse>> {
    const formData = new FormData()
    formData.append('file', file, file.name)
    return requestForm(
      `/api/v1/test-workspaces/${workspaceId}/cases/${caseId}/attachments`,
      formData,
      { method: 'POST', headers },
    )
  },

  listTestCaseAttachments(
    workspaceId: string,
    caseId: string,
  ): Promise<ApiResult<TestCaseAttachmentResponse[]>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/cases/${caseId}/attachments`)
  },

  deleteTestCaseAttachment(
    workspaceId: string,
    caseId: string,
    attachmentId: string,
    headers: HeadersInit,
  ): Promise<ApiResult<void>> {
    return request(
      `/api/v1/test-workspaces/${workspaceId}/cases/${caseId}/attachments/${attachmentId}`,
      { method: 'DELETE', headers },
    )
  },

  testCaseAttachmentContentUrl(
    workspaceId: string,
    caseId: string,
    attachmentId: string,
    inline = false,
  ): string {
    const suffix = inline ? '?inline=true' : ''
    return `/api/v1/test-workspaces/${workspaceId}/cases/${caseId}/attachments/${attachmentId}/content${suffix}`
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

  rerunTestRun(
    workspaceId: string,
    runId: string,
    headers: HeadersInit,
  ): Promise<ApiResult<TestRunResponse>> {
    return request(`/api/v1/test-workspaces/${workspaceId}/runs/${runId}/rerun`, {
      method: 'POST', headers,
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

  uploadTestRunItemAttachment(
    workspaceId: string,
    runId: string,
    itemId: string,
    file: File,
    headers: HeadersInit,
  ): Promise<ApiResult<TestRunItemAttachmentResponse>> {
    const formData = new FormData()
    formData.append('file', file, file.name)
    return requestForm(
      `/api/v1/test-workspaces/${workspaceId}/runs/${runId}/items/${itemId}/attachments`,
      formData,
      { method: 'POST', headers },
    )
  },

  listTestRunItemAttachments(
    workspaceId: string,
    runId: string,
    itemId: string,
  ): Promise<ApiResult<TestRunItemAttachmentResponse[]>> {
    return request(
      `/api/v1/test-workspaces/${workspaceId}/runs/${runId}/items/${itemId}/attachments`,
    )
  },

  deleteTestRunItemAttachment(
    workspaceId: string,
    runId: string,
    itemId: string,
    attachmentId: string,
    headers: HeadersInit,
  ): Promise<ApiResult<void>> {
    return request(
      `/api/v1/test-workspaces/${workspaceId}/runs/${runId}/items/${itemId}/attachments/${attachmentId}`,
      { method: 'DELETE', headers },
    )
  },

  testRunItemAttachmentContentUrl(
    workspaceId: string,
    runId: string,
    itemId: string,
    attachmentId: string,
    inline = false,
  ): string {
    const suffix = inline ? '?inline=true' : ''
    return `/api/v1/test-workspaces/${workspaceId}/runs/${runId}/items/${itemId}/attachments/${attachmentId}/content${suffix}`
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
