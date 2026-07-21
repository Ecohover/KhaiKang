import { describe, expect, it } from 'vitest'
import { problemMessage } from './client'

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
