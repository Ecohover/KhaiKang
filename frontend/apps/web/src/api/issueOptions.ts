import { apiClient } from './client'
import type {
  ApiProblem,
  IssueListQuery,
  IssueResponse,
  TestWorkspaceProjectResponse,
} from './contracts'

export interface IssueOptionsResult {
  issues: IssueResponse[]
  projects: TestWorkspaceProjectResponse[]
  error?: ApiProblem
}

export async function listAllProjectIssues(
  projectId: string,
  filters: IssueListQuery = {},
): Promise<{ issues: IssueResponse[], error?: ApiProblem }> {
  const issues: IssueResponse[] = []
  let page = 1
  let hasNextPage = true

  while (hasNextPage) {
    const result = await apiClient.listIssues(projectId, page, 100, filters)
    if (!result.data) {
      return result.error ? { issues, error: result.error } : { issues }
    }
    issues.push(...result.data.items)
    hasNextPage = result.data.hasNextPage
    page += 1
  }

  return { issues }
}

export async function listWorkspaceIssues(
  workspaceId: string,
  filters: IssueListQuery = {},
): Promise<IssueOptionsResult> {
  const projectResult = await apiClient.listTestWorkspaceProjects(workspaceId)
  if (!projectResult.data) {
    return projectResult.error
      ? { projects: [], issues: [], error: projectResult.error }
      : { projects: [], issues: [] }
  }

  const issueResults = await Promise.all(projectResult.data.map(
    project => listAllProjectIssues(project.projectId, filters),
  ))
  const error = issueResults.find(result => result.error)?.error
  const response = {
    projects: projectResult.data,
    issues: issueResults.flatMap(result => result.issues),
  }
  return error ? { ...response, error } : response
}
