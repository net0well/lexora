import { afterEach, describe, expect, it, vi } from 'vitest'

import en from '../en.json'
import ptBR from '../pt-BR.json'
import { createAppI18n, rememberLocale, storedLocale } from '../index'

function keysOf(messages: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(messages).flatMap(([key, value]) =>
    typeof value === 'object' && value !== null
      ? keysOf(value as Record<string, unknown>, `${prefix}${key}.`)
      : [`${prefix}${key}`],
  )
}

describe('i18n', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('starts in Portuguese when no language was chosen', () => {
    expect(storedLocale()).toBe('pt-BR')
  })

  it('ignores an unknown stored language', () => {
    localStorage.setItem('lexora.locale', 'fr')

    expect(storedLocale()).toBe('pt-BR')
  })

  it('remembers the chosen language', () => {
    rememberLocale('en')

    expect(storedLocale()).toBe('en')
    expect(createAppI18n(storedLocale()).global.t('common.save')).toBe('Save')
  })

  it('still starts when the browser blocks the storage', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError')
    })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError')
    })

    expect(storedLocale()).toBe('pt-BR')
    expect(() => rememberLocale('en')).not.toThrow()
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
