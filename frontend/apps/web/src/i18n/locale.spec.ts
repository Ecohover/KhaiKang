import { describe, expect, it } from 'vitest'
import { en } from './locales/en'
import { zhTW } from './locales/zh-TW'
import { defaultLocale, normalizeLocale, resolveInitialLocale } from './locale'

function flattenKeys(value: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, child]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return child && typeof child === 'object'
      ? flattenKeys(child as Record<string, unknown>, path)
      : [path]
  })
}

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

  it('keeps English and Traditional Chinese message keys aligned', () => {
    expect(flattenKeys(zhTW)).toEqual(flattenKeys(en))
  })
})
