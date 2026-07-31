import { describe, expect, it } from 'vitest'
import { shouldWarnMissingCompletionSummary } from './issueWorkflow'

describe('shouldWarnMissingCompletionSummary', () => {
  it('warns when completing a task without a result', () => {
    expect(
      shouldWarnMissingCompletionSummary({ completionSummary: null }, 'completed'),
    ).toBe(true)
    expect(
      shouldWarnMissingCompletionSummary({ completionSummary: '   ' }, 'completed'),
    ).toBe(true)
  })

  it('does not warn when a completion result exists', () => {
    expect(
      shouldWarnMissingCompletionSummary(
        { completionSummary: 'Released to the test environment.' },
        'completed',
      ),
    ).toBe(false)
  })

  it('does not warn for a non-completed status', () => {
    expect(
      shouldWarnMissingCompletionSummary({ completionSummary: null }, 'in_progress'),
    ).toBe(false)
  })
})
