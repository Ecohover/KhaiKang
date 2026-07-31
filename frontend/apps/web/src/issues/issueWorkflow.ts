import type { IssueResponse } from '../api/contracts'

export function shouldWarnMissingCompletionSummary(
  issue: Pick<IssueResponse, 'completionSummary'>,
  nextStatusCode: string,
): boolean {
  return nextStatusCode === 'completed' && !issue.completionSummary?.trim()
}
