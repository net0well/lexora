import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { documentsApi } from '@/api/documents'
import { knowledgeBasesApi } from '@/api/knowledgeBases'
import type { KnowledgeBase } from '@/api/types/knowledgeBase'
import type { SearchHit } from '@/api/types/search'
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
    search: vi.fn(),
  },
}))
vi.mock('@/api/documents', () => ({
  documentsApi: {
    list: vi.fn(),
    get: vi.fn(),
    download: vi.fn(),
    reprocess: vi.fn(),
    delete: vi.fn(),
    upload: vi.fn(),
  },
}))

const auth = vi.mocked(authApi)
const bases = vi.mocked(knowledgeBasesApi)
const documents = vi.mocked(documentsApi)

const base: KnowledgeBase = {
  id: 'kb-1',
  name: 'Manuais',
  description: null,
  status: 'active',
  chunkMaxTokens: 800,
  chunkOverlapTokens: 120,
  documentCount: 1,
  createdAt: '2026-11-20T10:00:00Z',
  updatedAt: '2026-11-20T10:00:00Z',
}

const spill: SearchHit = {
  chunkId: 'chunk-1',
  documentId: 'doc-1',
  documentName: 'manual-seguranca.pdf',
  content: 'Vazamentos químicos: isole a área num raio de 50 metros.',
  pageNumber: 1,
  sheetName: null,
  sectionTitle: null,
  score: 0.0328,
  matchedBy: 'both',
}

async function openSearch(path = '/knowledge-bases/kb-1?tab=search') {
  return mountWithApp(App, { path })
}

async function searchFor(wrapper: Awaited<ReturnType<typeof openSearch>>['wrapper'], text: string) {
  await wrapper.find('form[role="search"] input').setValue(text)
  await wrapper.find('form[role="search"]').trigger('submit')
  await flushPromises()
}

describe('knowledge base search tab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    auth.refresh.mockResolvedValue(sessionAs('viewer'))
    bases.get.mockResolvedValue(ok(base))
    bases.search.mockResolvedValue(ok({ hits: [spill] }))
    documents.list.mockResolvedValue(
      ok({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }),
    )
  })

  it('invites to search before the first one and asks for the text', async () => {
    const { wrapper } = await openSearch()
    expect(wrapper.text()).toContain('Busque nos documentos desta base')

    await wrapper.find('form[role="search"]').trigger('submit')

    expect(wrapper.text()).toContain('Digite o que buscar.')
    expect(bases.search).not.toHaveBeenCalled()
  })

  it('searches, keeps the search in the address and lists the excerpts with their source', async () => {
    const { wrapper, router } = await openSearch()

    await searchFor(wrapper, '  vazamentos químicos ')

    expect(router.currentRoute.value.query).toMatchObject({
      tab: 'search',
      q: 'vazamentos químicos',
    })
    expect(bases.search).toHaveBeenCalledWith(
      'kb-1',
      { query: 'vazamentos químicos', mode: 'hybrid' },
      expect.any(AbortSignal),
    )
    expect(wrapper.text()).toContain('1 trecho encontrado')
    expect(wrapper.text()).toContain('manual-seguranca.pdf')
    expect(wrapper.text()).toContain('Página 1')
    expect(wrapper.text()).toContain('Semântica e palavras-chave')
    expect(wrapper.text()).toContain('isole a área num raio de 50 metros')
  })

  it('runs the search of a shared link when the page opens', async () => {
    await openSearch('/knowledge-bases/kb-1?tab=search&q=epis&mode=keyword')

    expect(bases.search).toHaveBeenCalledWith(
      'kb-1',
      { query: 'epis', mode: 'keyword' },
      expect.any(AbortSignal),
    )
  })

  it('searches again with the chosen mode', async () => {
    const { wrapper, router } = await openSearch()
    await searchFor(wrapper, 'vazamentos')

    await buttonByText(wrapper.element, 'Semântica')!.click()
    await flushPromises()

    expect(router.currentRoute.value.query.mode).toBe('vector')
    expect(bases.search).toHaveBeenLastCalledWith(
      'kb-1',
      { query: 'vazamentos', mode: 'vector' },
      expect.any(AbortSignal),
    )
  })

  it('runs the same search again when it is submitted again', async () => {
    const { wrapper } = await openSearch()

    await searchFor(wrapper, 'vazamentos')
    await searchFor(wrapper, 'vazamentos')

    expect(bases.search).toHaveBeenCalledTimes(2)
  })

  it('says when nothing was found', async () => {
    bases.search.mockResolvedValue(ok({ hits: [] }))
    const { wrapper } = await openSearch()

    await searchFor(wrapper, 'férias')

    expect(wrapper.text()).toContain('Nenhum trecho encontrado')
    expect(wrapper.text()).toContain('Tente outras palavras')
  })

  it('downloads the document of an excerpt', async () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined)
    documents.download.mockResolvedValueOnce(
      ok({
        url: 'http://localhost:8333/lexora/doc-1?signature=x',
        expiresAt: '2026-11-20T10:05:00Z',
      }),
    )
    const { wrapper } = await openSearch()
    await searchFor(wrapper, 'vazamentos')

    await wrapper.find('article button[aria-label="Baixar"]').trigger('click')
    await flushPromises()

    expect(documents.download).toHaveBeenCalledWith('doc-1')
    expect((click.mock.contexts[0] as HTMLAnchorElement).href).toBe(
      'http://localhost:8333/lexora/doc-1?signature=x',
    )
  })
})
