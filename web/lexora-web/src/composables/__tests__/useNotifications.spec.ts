import { describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'

import { mountWithApp } from '@/__tests__/support'
import type { AppLocale } from '@/i18n'
import { useNotifications } from '../useNotifications'

const add = vi.fn()

vi.mock('primevue/usetoast', () => ({ useToast: () => ({ add }) }))

async function notificationsIn(locale: AppLocale) {
  let api!: ReturnType<typeof useNotifications>
  await mountWithApp(
    defineComponent({
      setup() {
        api = useNotifications()
        return () => null
      },
    }),
    { locale },
  )

  return api
}

describe('useNotifications', () => {
  it('translates a known key', async () => {
    const { describe: text } = await notificationsIn('pt-BR')

    expect(
      text({ key: 'knowledgeBase.name.duplicate', message: 'Duplicate.', type: 'conflict' }),
    ).toBe('Já existe uma base com esse nome.')
  })

  it('falls back to the server message for an unknown key', async () => {
    const { describe: text } = await notificationsIn('pt-BR')

    expect(
      text({ key: 'something.new', message: 'Something new happened.', type: 'businessRule' }),
    ).toBe('Something new happened.')
  })

  it('uses the server message in English', async () => {
    const { describe: text } = await notificationsIn('en')

    expect(
      text({
        key: 'knowledgeBase.name.duplicate',
        message: 'A knowledge base with this name already exists.',
        type: 'conflict',
      }),
    ).toBe('A knowledge base with this name already exists.')
  })

  it('shows validation problems as warnings and failures as errors', async () => {
    add.mockClear()
    const { notify } = await notificationsIn('en')

    notify([
      { key: 'search.query.required', message: 'Required.', type: 'validation' },
      {
        key: 'ai.unavailable',
        message: 'The AI provider is unavailable right now.',
        type: 'unavailable',
      },
    ])

    expect(add.mock.calls.map(([toast]) => toast.severity)).toEqual(['warn', 'error'])
  })
})
