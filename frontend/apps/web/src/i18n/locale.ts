export const supportedLocales = ['zh-TW', 'en'] as const
export type AppLocale = typeof supportedLocales[number]

export const defaultLocale: AppLocale = 'zh-TW'
export const localeStorageKey = 'khaikang.locale'

export function normalizeLocale(value: string | null | undefined): AppLocale | undefined {
  if (!value) return undefined
  const normalized = value.toLowerCase()
  if (normalized === 'zh-tw' || normalized.startsWith('zh-hant')) return 'zh-TW'
  if (normalized === 'en' || normalized.startsWith('en-')) return 'en'
  return undefined
}

export function resolveInitialLocale(
  storedLocale?: string | null,
  browserLocales: readonly string[] = [],
): AppLocale {
  return normalizeLocale(storedLocale)
    ?? browserLocales.map(normalizeLocale).find((locale): locale is AppLocale => Boolean(locale))
    ?? defaultLocale
}

export function readInitialLocale(): AppLocale {
  if (typeof window === 'undefined') return defaultLocale
  return resolveInitialLocale(
    window.localStorage.getItem(localeStorageKey),
    window.navigator.languages.length ? window.navigator.languages : [window.navigator.language],
  )
}

export function persistLocale(locale: AppLocale): void {
  if (typeof window !== 'undefined') window.localStorage.setItem(localeStorageKey, locale)
  if (typeof document !== 'undefined') document.documentElement.lang = locale
}
