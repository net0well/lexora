import { afterEach, describe, expect, it } from 'vitest'

import en from '../en.json'
import ptBR from '../pt-BR.json'
import { currentLocale, i18n, setLocale, storedLocale } from '../index'

function keysOf(messages: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(messages).flatMap(([key, value]) =>
    typeof value === 'object' && value !== null
      ? keysOf(value as Record<string, unknown>, `${prefix}${key}.`)
      : [`${prefix}${key}`],
  )
}

describe('i18n', () => {
  afterEach(() => {
    localStorage.clear()
    setLocale('pt-BR')
  })

  it('starts in Portuguese when no language was chosen', () => {
    expect(storedLocale()).toBe('pt-BR')
  })

  it('ignores an unknown stored language', () => {
    localStorage.setItem('lexora.locale', 'fr')

    expect(storedLocale()).toBe('pt-BR')
  })

  it('switches and remembers the chosen language', () => {
    setLocale('en')

    expect(currentLocale()).toBe('en')
    expect(storedLocale()).toBe('en')
    expect(i18n.global.t('common.save')).toBe('Save')
    expect(document.documentElement.lang).toBe('en')
  })

  it('has every interface text in both languages', () => {
    const interfaceKeys = (messages: Record<string, unknown>) =>
      keysOf(messages).filter((key) => !key.startsWith('notifications.'))

    expect(interfaceKeys(en).sort()).toEqual(interfaceKeys(ptBR).sort())
  })

  it('translates in Portuguese the notifications created by the client', () => {
    const notifications = keysOf(ptBR).filter((key) => key.startsWith('notifications.'))

    expect(notifications).toEqual(
      expect.arrayContaining(keysOf(en).filter((key) => key.startsWith('notifications.'))),
    )
  })
})
