import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { authApi } from '@/api/auth'
import { conversationsApi } from '@/api/conversations'
import { documentsApi } from '@/api/documents'
import { knowledgeBasesApi } from '@/api/knowledgeBases'
import { ok, session } from '@/__tests__/tokens'
import { useAuthStore } from '../auth'
import { useConversationStore } from '../conversation'
import { useDocumentsStore } from '../documents'
import { useKnowledgeBasesStore } from '../knowledgeBases'

vi.mock('@/api/auth', () => ({
  authApi: { login: vi.fn(), refresh: vi.fn(), logout: vi.fn() },
}))
vi.mock('@/api/knowledgeBases', () => ({ knowledgeBasesApi: { list: vi.fn(), get: vi.fn() } }))
vi.mock('@/api/documents', () => ({ documentsApi: { list: vi.fn() } }))
vi.mock('@/api/conversations', () => ({ conversationsApi: { list: vi.fn() } }))

function page<T>(items: T[]) {
  return ok({ items, page: 1, pageSize: 20, totalItems: items.length, totalPages: 1 })
}

describe('stores of the signed in user', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('forget what the previous user saw when another user signs in', async () => {
    const ana = session('admin')
    const bruno = { ...session('viewer'), user: { ...ana.user, id: 'user-2', name: 'Bruno' } }
    vi.mocked(authApi.login).mockResolvedValueOnce(ok(ana)).mockResolvedValueOnce(ok(bruno))
    vi.mocked(knowledgeBasesApi.list).mockResolvedValue(page([{ id: 'kb-1' }] as never[]))
    vi.mocked(documentsApi.list).mockResolvedValue(page([{ id: 'doc-1' }] as never[]))
    vi.mocked(conversationsApi.list).mockResolvedValue(page([{ id: 'conv-1' }] as never[]))
    const auth = useAuthStore()
    const bases = useKnowledgeBasesStore()
    const documents = useDocumentsStore()
    const conversations = useConversationStore()
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })
    await Promise.all([bases.load({}), documents.load('kb-1', {}), conversations.loadList('kb-1')])

    await auth.login({ email: 'bruno@lexora.local', password: 'secret' })

    expect(bases.items).toEqual([])
    expect(documents.items).toEqual([])
    expect(conversations.conversations).toEqual([])
  })
})
