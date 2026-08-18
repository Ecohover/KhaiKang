// Keep these wire types aligned with contract/openapi/khaikang.v1.yaml.
// They are maintained source code so project-specific behavior can evolve without
// a generator overwriting it.

export interface SetupStatusResponse {
  requiresInitialization: boolean
}

export interface InitializeAdminResponse {
  username: string
  initialPassword: string
}

export interface CsrfTokenResponse {
  token: string
}

export interface LoginRequest {
  username: string
  password: string
  rememberMe: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface AuthenticatedUserResponse {
  id: string
  username: string
  systemRoles: string[]
  systemPermissions: string[]
  mustChangePassword: boolean
}

export type AccountStatus = 'active' | 'suspended' | 'disabled'

export interface AccountResponse {
  id: string
  username: string
  accountType: 'human' | 'ai_agent'
  status: AccountStatus
  systemRoles: string[]
  mustChangePassword: boolean
  lastLoginAt: string | null
  createdAt: string
  updatedAt: string
  version: number
}

export interface CreateAccountRequest {
  username: string
}

export interface CreateAccountResponse {
  account: AccountResponse
  initialPassword: string
}

export interface UpdateAccountStatusRequest {
  status: AccountStatus
  version: number
}

export interface UpdateAccountRequest {
  username: string
  version: number
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface CreateProjectRequest {
  code: string
  name: string
  description: string | null
}

export interface UpdateProjectRequest {
  name: string
  description: string | null
  status: 'active' | 'inactive'
  version: number
}

export interface ProjectResponse {
  id: string
  code: string
  name: string
  description: string | null
  status: 'active' | 'inactive'
  currentUserRoles: string[]
  currentUserPermissions: string[]
  createdAt: string
  updatedAt: string
  version: number
}

export interface ProjectRoleResponse {
  code: string
  name: string
  description: string
}

export interface ProjectMemberResponse {
  id: string
  accountId: string
  username: string
  status: 'active' | 'removed'
  roleCodes: string[]
  joinedAt: string
  version: number
}

export interface AddProjectMemberRequest {
  username: string
  roleCodes: string[]
}

export interface UpdateProjectMemberRolesRequest {
  roleCodes: string[]
  version: number
}

export interface IssueOptionResponse {
  code: string
  name: string
  description: string | null
  category: string | null
}

export interface IssueMetadataResponse {
  types: IssueOptionResponse[]
  statuses: IssueOptionResponse[]
  priorities: IssueOptionResponse[]
}

export interface CreateIssueRequest {
  title: string
  typeCode: string
  priorityCode?: string | null
  description?: string | null
  userStory?: string | null
  definitionOfDone?: string | null
  assigneeAccountId?: string | null
}

export interface UpdateIssueStatusRequest {
  statusCode: string
  version: number
}

export interface UpdateIssueRequest {
  title: string
  typeCode: string
  priorityCode: string
  description?: string | null
  userStory?: string | null
  definitionOfDone?: string | null
  completionSummary?: string | null
  version: number
}

export interface UpdateIssueAssigneeRequest {
  assigneeAccountId: string | null
  version: number
}

export interface IssueResponse {
  id: string
  projectId: string
  issueNo: number
  key: string
  title: string
  description: string | null
  userStory: string | null
  definitionOfDone: string | null
  completionSummary: string | null
  typeCode: string
  typeName: string
  statusCode: string
  statusName: string
  priorityCode: string
  priorityName: string
  reporterAccountId: string
  reporterUsername: string
  assigneeAccountId: string | null
  assigneeUsername: string | null
  completedAt: string | null
  createdAt: string
  updatedAt: string
  version: number
}

export interface ApiProblem {
  type?: string | null
  title?: string | null
  status?: number | null
  detail?: string | null
  instance?: string | null
  code?: string | null
  errors?: Record<string, string[]>
}

export type TestWorkspaceRole = 'owner' | 'manager' | 'tester' | 'viewer'

export interface TestWorkspaceResponse {
  id: string
  name: string
  prefix: string
  description: string | null
  status: 'active' | 'inactive'
  currentUserRole: TestWorkspaceRole
  createdAt: string
  updatedAt: string
  version: number
}

export type IssueRelationDirectionKind = 'symmetric' | 'directed' | 'hierarchical'
export type IssueRelationDirection = 'forward' | 'reverse'

export interface IssueRelationTypeResponse {
  id: string
  code: string
  forwardLabel: string
  reverseLabel: string
  directionKind: IssueRelationDirectionKind
}

export interface IssueRelationIssueResponse {
  id: string
  issueNo: number
  key: string
  title: string
  typeCode: string
  statusCode: string
}

export interface IssueRelationResponse {
  id: string
  projectId: string
  relationTypeCode: string
  forwardLabel: string
  reverseLabel: string
  directionKind: IssueRelationDirectionKind
  sourceIssue: IssueRelationIssueResponse
  targetIssue: IssueRelationIssueResponse
  createdAt: string
  version: number
}

export interface CreateIssueRelationRequest {
  relationTypeCode: string
  relatedIssueId: string
  direction: IssueRelationDirection
}

export interface LinkTestWorkspaceProjectRequest {
  projectId: string
}

export interface TestWorkspaceProjectResponse {
  id: string
  projectId: string
  code: string
  name: string
  status: 'active' | 'inactive'
  linkedAt: string
  version: number
}

export interface CreateTestWorkspaceRequest {
  name: string
  prefix?: string | null
  description: string | null
}

export interface UpdateTestWorkspaceRequest {
  name: string
  description: string | null
  status: 'active' | 'inactive'
  version: number
}

export interface TestWorkspaceMemberResponse {
  id: string
  accountId: string
  username: string
  role: TestWorkspaceRole
  status: string
  joinedAt: string
  version: number
}

export interface AddTestWorkspaceMemberRequest {
  username: string
  role: TestWorkspaceRole
}

export interface UpdateTestWorkspaceMemberRequest {
  role: TestWorkspaceRole
  version: number
}

export interface TestSuiteResponse {
  id: string
  parentId: string | null
  name: string
  description: string | null
  sortOrder: number
  status: 'active' | 'inactive'
  depth: number
  version: number
}

export interface CreateTestSuiteRequest {
  parentId: string | null
  name: string
  description: string | null
  sortOrder: number
}

export interface UpdateTestSuiteRequest extends CreateTestSuiteRequest {
  status: 'active' | 'inactive'
  version: number
}

export interface CreateTestCaseStepRequest {
  action: string
  expectedResult: string
}

export interface CreateTestCaseRequest {
  suiteId: string
  title: string
  description?: string | null
  preconditions?: string | null
  overallExpectedResult?: string | null
  sortOrder: number
  steps: CreateTestCaseStepRequest[]
  tagIds?: string[] | null
}

export interface UpdateTestCaseRequest {
  suiteId: string
  title: string
  description?: string | null
  preconditions?: string | null
  overallExpectedResult?: string | null
  sortOrder: number
  status: 'active' | 'inactive'
  version: number
  steps: CreateTestCaseStepRequest[]
  tagIds?: string[] | null
}

export interface IssueListQuery {
  search?: string
  typeCode?: string
  statusCode?: string
  priorityCode?: string
  assigneeAccountId?: string
  unassigned?: boolean
  sortBy?: 'updatedAt' | 'issueNo'
  sortDirection?: 'asc' | 'desc'
}

export interface TestTagResponse {
  id: string
  name: string
  description: string | null
  status: 'active' | 'inactive'
  version: number
}

export interface IssueAttachmentResponse {
  id: string
  issueId: string
  originalFileName: string
  contentType: string
  fileSize: number
  fileHash: string
  uploadedByAccountId: string
  uploadedByUsername: string
  createdAt: string
}

export interface CreateTestTagRequest {
  name: string
  description: string | null
}

export interface UpdateTestTagRequest extends CreateTestTagRequest {
  status: 'active' | 'inactive'
  version: number
}

export interface TestCaseStepResponse {
  id: string
  stepNo: number
  action: string
  expectedResult: string
}

export interface TestCaseResponse {
  id: string
  suiteId: string
  caseNo: number
  tags: TestTagResponse[]
  title: string
  description: string | null
  preconditions: string | null
  overallExpectedResult: string | null
  sortOrder: number
  status: 'active' | 'inactive'
  steps: TestCaseStepResponse[]
  createdAt: string
  updatedAt: string
  version: number
}

export interface TestCaseAttachmentResponse {
  id: string
  testCaseId: string
  originalFileName: string
  contentType: string
  fileSize: number
  fileHash: string
  uploadedByAccountId: string
  uploadedByUsername: string
  createdAt: string
}

export interface TestRunItemAttachmentResponse {
  id: string
  testRunItemId: string
  originalFileName: string
  contentType: string
  fileSize: number
  fileHash: string
  uploadedByAccountId: string
  uploadedByUsername: string
  createdAt: string
}

export type TestPlanStatus = 'draft' | 'active' | 'archived'

export interface CreateTestPlanRequest {
  name?: string | null
  description: string | null
  caseIds: string[]
  testIssueId?: string | null
}

export interface UpdateTestPlanRequest extends CreateTestPlanRequest {
  status: TestPlanStatus
  version: number
}

export interface TestPlanItemResponse {
  id: string
  caseId: string
  sortOrder: number
  caseTitle: string
}

export interface TestPlanResponse {
  id: string
  workspaceId: string
  planNo: number
  code: string
  name: string
  description: string | null
  status: TestPlanStatus
  items: TestPlanItemResponse[]
  createdAt: string
  updatedAt: string
  version: number
  testIssue: TestTraceIssueResponse | null
}

export type TestResultStatus = 'not_run' | 'passed' | 'failed' | 'blocked' | 'skipped'
export type TestRunStatus = 'not_started' | 'in_progress' | 'completed' | 'cancelled'

export interface CreateTestRunRequest {
  planId: string
  name: string
}

export interface UpdateTestRunStatusRequest {
  status: 'in_progress' | 'completed' | 'cancelled'
  summary: string | null
  version: number
}

export interface RecordTestResultRequest {
  status: TestResultStatus
  actualResult: string | null
  version: number
}

export interface TestRunStepResponse {
  id: string
  stepNo: number
  action: string
  expectedResult: string
  resultStatus: TestResultStatus
  actualResult: string | null
  executedByAccountId: string | null
  executedAt: string | null
  version: number
}

export interface TestRunItemResponse {
  id: string
  caseId: string
  sortOrder: number
  caseTitle: string
  caseDescription: string | null
  preconditions: string | null
  overallExpectedResult: string | null
  resultStatus: TestResultStatus
  actualResult: string | null
  executedByAccountId: string | null
  executedAt: string | null
  steps: TestRunStepResponse[]
  version: number
}

export interface TestRunProgressResponse {
  total: number
  notRun: number
  passed: number
  failed: number
  blocked: number
  skipped: number
}

export interface TestRunResponse {
  id: string
  planId: string
  runNo: number
  code: string
  name: string
  status: TestRunStatus
  startedByAccountId: string
  startedAt: string | null
  completedAt: string | null
  summary: string | null
  progress: TestRunProgressResponse
  items: TestRunItemResponse[]
  createdAt: string
  updatedAt: string
  version: number
  testIssue: TestTraceIssueResponse | null
}

export interface TestTraceIssueResponse {
  id: string
  projectId: string
  projectCode: string
  issueNo: number
  key: string
  title: string
  typeCode: string
  statusCode: string
}

export interface LinkTestCaseRequirementIssueRequest {
  requirementIssueId: string
}

export interface TestCaseRequirementLinkResponse {
  id: string
  testCaseId: string
  issue: TestTraceIssueResponse
  createdAt: string
  version: number
}

export interface CreateTestRunBugRequest {
  projectId: string
  title: string
  priorityCode: string | null
  description: string | null
  assigneeAccountId: string | null
}

export interface TestRunBugLinkResponse {
  id: string
  testRunId: string
  issue: TestTraceIssueResponse
  createdAt: string
  version: number
}
