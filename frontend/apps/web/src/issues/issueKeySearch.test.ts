import { describe, expect, it } from 'vitest'
import { findIssueByExactKey } from './issueKeySearch'

const issues = [
  { id: '1', key: 'FP-1', title: '規劃任務' },
  { id: '2', key: 'FP-2', title: '測試任務' },
]

describe('findIssueByExactKey', () => {
  it('matches a complete key while ignoring whitespace and letter case', () => {
    expect(findIssueByExactKey(issues, ' fp-2 ')?.id).toBe('2')
  })

  it('does not match a partial key', () => {
    expect(findIssueByExactKey(issues, 'FP-')).toBeUndefined()
    expect(findIssueByExactKey(issues, '2')).toBeUndefined()
  })

  it('does not match an issue title', () => {
    expect(findIssueByExactKey(issues, '測試任務')).toBeUndefined()
  })
})
