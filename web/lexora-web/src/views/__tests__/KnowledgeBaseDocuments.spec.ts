import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { documentsApi } from '@/api/documents'
import { knowledgeBasesApi } from '@/api/knowledgeBases'
import type { DocumentListItem } from '@/api/types/document'
import type { KnowledgeBase } from '@/api/types/knowledgeBase'
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

const manual: DocumentListItem = {
  id: 'doc-1',
  fileName: 'manual-seguranca.pdf',
  format: 'pdf',
  sizeBytes: 2048,
  status: 'indexed',
  chunkCount: 3,
  embeddedChunkCount: 3,
  uploadedAt: '2026-11-20T10:00:00Z',
  updatedAt: '2026-11-20T10:05:00Z',
}

function page(items: DocumentListItem[]) {
  return ok({ items, page: 1, pageSize: 20, totalItems: items.length, totalPages: 1 })
}

describe('knowledge base documents tab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    bases.get.mockResolvedValue(ok(base))
    documents.list.mockResolvedValue(page([manual]))
  })

  it('lists the documents of the base with their status', async () => {
    auth.refresh.mockResolvedValue(sessionAs('viewer'))

    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })

    expect(documents.list).toHaveBeenCalledWith(
      'kb-1',
      { status: null, search: '', page: 1, pageSize: 20 },
      expect.any(AbortSignal),
    )
    expect(wrapper.text()).toContain('manual-seguranca.pdf')
    expect(wrapper.text()).toContain('Indexado')
    expect(wrapper.text()).toContain('2 KB')
  })

  it('lets a member upload but not a viewer', async () => {
    auth.refresh.mockResolvedValue(sessionAs('viewer'))
    const viewer = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })
    expect(
      viewer.wrapper
        .find('[role="button"][aria-label="Arraste arquivos para cá ou clique para escolher"]')
        .exists(),
    ).toBe(false)
    expect(viewer.wrapper.find('[aria-label="Excluir"]').exists()).toBe(false)

    auth.refresh.mockResolvedValue(sessionAs('member'))
    const member = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })
    expect(
      member.wrapper
        .find('[role="button"][aria-label="Arraste arquivos para cá ou clique para escolher"]')
        .exists(),
    ).toBe(true)
    expect(member.wrapper.find('[aria-label="Excluir"]').exists()).toBe(true)
  })

  it('does not offer the upload on an archived base', async () => {
    auth.refresh.mockResolvedValue(sessionAs('admin'))
    bases.get.mockResolvedValue(ok({ ...base, status: 'archived' }))

    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })

    expect(
      wrapper
        .find('[role="button"][aria-label="Arraste arquivos para cá ou clique para escolher"]')
        .exists(),
    ).toBe(false)
    expect(wrapper.text()).toContain('A base está arquivada e não recebe documentos.')
  })

  it('adds an uploaded file to the table and to the document count', async () => {
    auth.refresh.mockResolvedValue(sessionAs('member'))
    documents.upload.mockResolvedValueOnce(
      ok(
        {
          id: 'doc-2',
          fileName: 'politica.docx',
          format: 'docx',
          status: 'uploaded',
          sizeBytes: 10,
        },
        202,
      ),
    )
    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })
    const input = wrapper.find<HTMLInputElement>('input[type="file"]')
    Object.defineProperty(input.element, 'files', {
      value: [new File(['x'.repeat(10)], 'politica.docx')],
    })

    await input.trigger('change')
    await flushPromises()

    expect(documents.upload).toHaveBeenCalledWith('kb-1', expect.any(File), expect.any(Object))
    expect(wrapper.text()).toContain('politica.docx')
    expect(wrapper.text()).toContain('2 documentos')
  })

  it('searches by name after the typing stops', async () => {
    vi.useFakeTimers()
    auth.refresh.mockResolvedValue(sessionAs('viewer'))
    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1' })

    await wrapper.find('input[aria-label="Buscar por nome"]').setValue('manual')
    await vi.advanceTimersByTimeAsync(300)

    expect(documents.list).toHaveBeenLastCalledWith(
      'kb-1',
      { status: null, search: 'manual', page: 1, pageSize: 20 },
      expect.any(AbortSignal),
    )
  })
})
