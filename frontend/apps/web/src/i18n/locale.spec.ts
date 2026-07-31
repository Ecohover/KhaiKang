import { describe, expect, it } from 'vitest'
import { defaultLocale, normalizeLocale, resolveInitialLocale } from './locale'

describe('locale resolution', () => {
  it.each([
    ['zh-TW', 'zh-TW'],
    ['zh-Hant-TW', 'zh-TW'],
    ['en', 'en'],
    ['en-US', 'en'],
  ] as const)('normalizes %s to %s', (input, expected) => {
    expect(normalizeLocale(input)).toBe(expected)
  })

  it('prefers a saved supported locale', () => {
    expect(resolveInitialLocale('en', ['zh-TW'])).toBe('en')
  })

  it('uses the first supported browser locale', () => {
    expect(resolveInitialLocale(null, ['ja-JP', 'en-US', 'zh-TW'])).toBe('en')
  })

  it('falls back to the product default', () => {
    expect(resolveInitialLocale(null, ['ja-JP'])).toBe(defaultLocale)
  })
})
