export interface IssueKeyCandidate {
  key: string
}

export function findIssueByExactKey<T extends IssueKeyCandidate>(
  issues: readonly T[],
  query: string,
): T | undefined {
  const normalizedQuery = normalizeIssueKey(query)
  return issues.find(issue => normalizeIssueKey(issue.key) === normalizedQuery)
}

function normalizeIssueKey(value: string): string {
  return value.trim().toLocaleUpperCase()
}
