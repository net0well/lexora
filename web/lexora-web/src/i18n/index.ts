import { createI18n } from 'vue-i18n'

import en from './en.json'
import ptBR from './pt-BR.json'

export const supportedLocales = ['pt-BR', 'en'] as const

export type AppLocale = (typeof supportedLocales)[number]

export const defaultLocale: AppLocale = 'pt-BR'

const storageKey = 'lexora.locale'

export function isSupportedLocale(value: unknown): value is AppLocale {
  return supportedLocales.includes(value as AppLocale)
}

// Storage can be blocked (privacy settings, sandboxed frames): the language is only a preference, so the
// app starts in the default one instead of failing.
export function storedLocale(): AppLocale {
  try {
    const stored = globalThis.localStorage?.getItem(storageKey)

    return isSupportedLocale(stored) ? stored : defaultLocale
  } catch {
    return defaultLocale
  }
}

export function createAppI18n(locale: AppLocale = storedLocale()) {
  return createI18n({
    legacy: false,
    locale,
    fallbackLocale: 'en',
    messages: { 'pt-BR': ptBR, en },
    missingWarn: false,
    fallbackWarn: false,
  })
}

export const i18n = createAppI18n()

export function rememberLocale(locale: AppLocale): void {
  try {
    globalThis.localStorage?.setItem(storageKey, locale)
  } catch {
    return
  }
}
