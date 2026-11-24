import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { knowledgeBasesApi } from '@/api/knowledgeBases'
import type { KnowledgeBaseListItem } from '@/api/types/knowledgeBase'
import { mountWithApp } from '@/__tests__/support'
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

const manuals: KnowledgeBaseListItem = {
  id: 'kb-1',
  name: 'Manuais de segurança',
  description: 'Procedimentos do laboratório',
  status: 'active',
  documentCount: 3,
  updatedAt: '2026-11-20T10:00:00Z',
}

function page(items: KnowledgeBaseListItem[]) {
  return ok({ items, page: 1, pageSize: 24, totalItems: items.length, totalPages: 1 })
}

describe('KnowledgeBasesView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    bases.list.mockResolvedValue(page([manuals]))
  })

  it('lists the active bases with their document count', async () => {
    auth.refresh.mockResolvedValue(sessionAs('viewer'))

    const { wrapper } = await mountWithApp(App, { path: '/' })

    expect(bases.list).toHaveBeenCalledWith(
      { status: 'active', page: 1, pageSize: 24 },
      expect.any(AbortSignal),
    )
    expect(wrapper.text()).toContain('Manuais de segurança')
    expect(wrapper.text()).toContain('3 documentos')
    expect(
      wrapper
        .findAll('a')
        .find((link) => link.text().includes('Manuais de segurança'))
        ?.attributes('href'),
    ).toBe('/knowledge-bases/kb-1')
  })

  it('keeps the filter and the page in the address', async () => {
    auth.refresh.mockResolvedValue(sessionAs('viewer'))
    const { wrapper, router } = await mountWithApp(App, { path: '/?status=archived&page=2' })

    expect(bases.list).toHaveBeenCalledWith(
      { status: 'archived', page: 2, pageSize: 24 },
      expect.any(AbortSignal),
    )

    await wrapper
      .find('[aria-label="Filtrar por status"]')
      .findAll('[role="button"], button')
      .find((option) => option.text() === 'Ativas')!
      .trigger('click')
    await vi.waitFor(() => expect(router.currentRoute.value.fullPath).toBe('/'))
    expect(bases.list).toHaveBeenLastCalledWith(
      { status: 'active', page: 1, pageSize: 24 },
      expect.any(AbortSignal),
    )
  })

  it('lets only an admin create a base', async () => {
    auth.refresh.mockResolvedValue(sessionAs('member'))

    const { wrapper } = await mountWithApp(App, { path: '/' })

    expect(wrapper.text()).not.toContain('Nova base')
  })

  it('creates a base and opens it', async () => {
    auth.refresh.mockResolvedValue(sessionAs('admin'))
    bases.create.mockResolvedValueOnce(
      ok({
        id: 'kb-2',
        name: 'Políticas',
        description: null,
        chunkMaxTokens: 800,
        chunkOverlapTokens: 120,
      }),
    )
    bases.get.mockResolvedValue(
      ok({
        ...manuals,
        id: 'kb-2',
        name: 'Políticas',
        chunkMaxTokens: 800,
        chunkOverlapTokens: 120,
        createdAt: manuals.updatedAt,
      }),
    )
    const { wrapper, router } = await mountWithApp(App, { path: '/' })

    await wrapper
      .findAll('button')
      .find((button) => button.text().includes('Nova base'))!
      .trigger('click')
    await flushPromises()
    const name = document.querySelector<HTMLInputElement>('#new-knowledge-base-name')!
    name.value = 'Políticas'
    name.dispatchEvent(new Event('input'))
    document
      .querySelector<HTMLFormElement>('[role="dialog"] form')!
      .dispatchEvent(new Event('submit'))
    await flushPromises()

    expect(bases.create).toHaveBeenCalledWith({
      name: 'Políticas',
      description: null,
      chunkMaxTokens: 800,
      chunkOverlapTokens: 120,
    })
    await vi.waitFor(() => expect(router.currentRoute.value.fullPath).toBe('/knowledge-bases/kb-2'))
  })

  it('shows an empty state when there are no bases', async () => {
    auth.refresh.mockResolvedValue(sessionAs('admin'))
    bases.list.mockResolvedValue(page([]))

    const { wrapper } = await mountWithApp(App, { path: '/' })

    expect(wrapper.text()).toContain('Nenhuma base de conhecimento ainda.')
    expect(wrapper.text()).toContain('Crie a primeira base')
  })
})
