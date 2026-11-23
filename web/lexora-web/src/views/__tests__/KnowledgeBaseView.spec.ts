import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { knowledgeBasesApi } from '@/api/knowledgeBases'
import type { KnowledgeBase } from '@/api/types/knowledgeBase'
import { buttonByText, mountWithApp } from '@/__tests__/support'
import { ok, sessionAs } from '@/__tests__/tokens'

vi.mock('@/api/auth', () => ({
  authApi: {
    login: vi.fn(),
    refresh: vi.fn(),
    logout: vi.fn(),
    me: vi.fn(),
    changePassword: vi.fn(),
  },
}))
vi.mock('@/api/knowledgeBases', () => ({
  knowledgeBasesApi: {
    list: vi.fn(),
    get: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    archive: vi.fn(),
    reindex: vi.fn(),
  },
}))

const auth = vi.mocked(authApi)
const bases = vi.mocked(knowledgeBasesApi)

const manuals: KnowledgeBase = {
  id: 'kb-1',
  name: 'Manuais de segurança',
  description: 'Procedimentos do laboratório',
  status: 'active',
  chunkMaxTokens: 800,
  chunkOverlapTokens: 120,
  documentCount: 1,
  createdAt: '2026-11-20T10:00:00Z',
  updatedAt: '2026-11-20T10:00:00Z',
}

describe('KnowledgeBaseView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    bases.get.mockResolvedValue(ok(manuals))
  })

  it('shows the base with its details', async () => {
    auth.refresh.mockResolvedValue(sessionAs('viewer'))

    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })

    expect(bases.get).toHaveBeenCalledWith('kb-1', expect.any(AbortSignal))
    expect(wrapper.find('h1').text()).toBe('Manuais de segurança')
    expect(wrapper.text()).toContain('1 documento')
    expect(wrapper.text()).not.toContain('Configurações')
  })

  it('drops the answer of a base the user already left', async () => {
    auth.refresh.mockResolvedValue(sessionAs('viewer'))
    let answerFirst!: () => void
    bases.get.mockImplementation((id, signal) =>
      id === 'kb-1'
        ? new Promise((resolve, reject) => {
            answerFirst = () => resolve(ok(manuals))
            signal?.addEventListener('abort', () =>
              reject(new DOMException('Aborted', 'AbortError')),
            )
          })
        : Promise.resolve(ok({ ...manuals, id: 'kb-2', name: 'Políticas' })),
    )
    const { wrapper, router } = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })

    await router.push('/knowledge-bases/kb-2')
    await flushPromises()
    answerFirst()
    await flushPromises()

    expect(wrapper.find('h1').text()).toBe('Políticas')
  })

  it('tells when the base does not exist', async () => {
    auth.refresh.mockResolvedValue(sessionAs('viewer'))
    bases.get.mockResolvedValue({
      success: false,
      data: null,
      notifications: [
        { key: 'knowledgeBase.notFound', message: 'Knowledge base not found.', type: 'notFound' },
      ],
      status: 404,
    })

    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/missing' })

    expect(wrapper.text()).toContain('Base de conhecimento não encontrada.')
  })

  it('gives the admin the settings to edit the base', async () => {
    auth.refresh.mockResolvedValue(sessionAs('admin'))
    bases.update.mockResolvedValueOnce(
      ok({
        id: 'kb-1',
        name: 'Manuais',
        description: null,
        chunkMaxTokens: 600,
        chunkOverlapTokens: 100,
      }),
    )

    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1?tab=settings' })
    await wrapper.find('#edit-knowledge-base-name').setValue('Manuais')
    await wrapper.find('#edit-knowledge-base-name').element.closest('form')!.requestSubmit()
    await flushPromises()

    expect(bases.update).toHaveBeenCalledWith('kb-1', expect.objectContaining({ name: 'Manuais' }))
    expect(wrapper.find('h1').text()).toBe('Manuais')
  })

  it('archives the base after confirmation', async () => {
    auth.refresh.mockResolvedValue(sessionAs('admin'))
    bases.archive.mockResolvedValueOnce(ok(null))
    const { wrapper } = await mountWithApp(App, {
      path: '/knowledge-bases/kb-1?tab=settings',
      attachTo: document.body,
    })

    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Arquivar')!
      .trigger('click')
    await flushPromises()
    buttonByText(
      document.querySelector('[role="alertdialog"], [role="dialog"]')!,
      'Arquivar',
    )!.click()
    await flushPromises()

    expect(bases.archive).toHaveBeenCalledWith('kb-1')
    expect(wrapper.text()).toContain('Arquivada')
  })
})
