import { watch } from 'vue'
import { createI18n } from 'vue-i18n'
import { en } from './locales/en'
import { zhTW } from './locales/zh-TW'
import { persistLocale, readInitialLocale, type AppLocale } from './locale'

export const i18n = createI18n({
  legacy: false,
  locale: readInitialLocale(),
  fallbackLocale: 'zh-TW',
  messages: {
    'zh-TW': zhTW,
    en,
  },
  datetimeFormats: {
    'zh-TW': {
      short: { year: 'numeric', month: '2-digit', day: '2-digit' },
      medium: { year: 'numeric', month: 'short', day: 'numeric' },
      dateTime: { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' },
    },
    en: {
      short: { year: 'numeric', month: '2-digit', day: '2-digit' },
      medium: { year: 'numeric', month: 'short', day: 'numeric' },
      dateTime: { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' },
    },
  },
  numberFormats: {
    'zh-TW': {
      integer: { maximumFractionDigits: 0 },
      decimal: { minimumFractionDigits: 0, maximumFractionDigits: 2 },
    },
    en: {
      integer: { maximumFractionDigits: 0 },
      decimal: { minimumFractionDigits: 0, maximumFractionDigits: 2 },
    },
  },
  missingWarn: import.meta.env.DEV,
  fallbackWarn: import.meta.env.DEV,
})

persistLocale(i18n.global.locale.value as AppLocale)
watch(i18n.global.locale, (locale) => persistLocale(locale as AppLocale))

export * from './locale'
