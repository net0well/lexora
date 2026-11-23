import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import App from '@/App.vue'
import { aiApi } from '@/api/ai'
import { authApi } from '@/api/auth'
import { conversationsApi } from '@/api/conversations'
import { documentsApi } from '@/api/documents'
import { knowledgeBasesApi } from '@/api/knowledgeBases'
import type { ApiResult } from '@/api/types/commandResult'
import type {
  AnswerStreamHandlers,
  Conversation,
  ConversationMessage,
} from '@/api/types/conversation'
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
    search: vi.fn(),
  },
}))
vi.mock('@/api/conversations', () => ({
  conversationsApi: {
    list: vi.fn(),
    get: vi.fn(),
    start: vi.fn(),
    delete: vi.fn(),
    stream: vi.fn(),
  },
}))
vi.mock('@/api/ai', () => ({ aiApi: { profiles: vi.fn() } }))
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
const conversations = vi.mocked(conversationsApi)
const ai = vi.mocked(aiApi)
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

function message(overrides: Partial<ConversationMessage>): ConversationMessage {
  return {
    id: 'message-1',
    role: 'assistant',
    content: '',
    citations: [],
    provider: 'ollama',
    model: 'qwen2.5:7b',
    inputTokens: 10,
    outputTokens: 5,
    latencyMs: 1500,
    createdAt: '2026-11-20T10:00:00Z',
    ...overrides,
  }
}

function conversation(messages: ConversationMessage[] = []): Conversation {
  return {
    id: 'conv-1',
    knowledgeBaseId: 'kb-1',
    title: 'Vazamentos',
    messages,
    createdAt: '2026-11-20T10:00:00Z',
    updatedAt: '2026-11-20T10:00:00Z',
  }
}

function streamingAnswer(): (
  id: string,
  request: unknown,
  handlers: AnswerStreamHandlers,
) => Promise<ApiResult<null>> {
  return async (_id, _request, handlers) => {
    handlers.delta('Isole a área ')
    handlers.delta('num raio de 50 metros [1].')
    handlers.citations({ citations: [], grounded: true })
    handlers.done({
      conversationId: 'conv-1',
      messageId: 'answer-1',
      provider: 'ollama',
      model: 'qwen2.5:7b',
      inputTokens: 300,
      outputTokens: 20,
      latencyMs: 2500,
    })
    return ok(null)
  }
}

async function ask(wrapper: Awaited<ReturnType<typeof mountWithApp>>['wrapper'], text: string) {
  await wrapper.find('textarea').setValue(text)
  await wrapper.find('form.composer').trigger('submit')
  await flushPromises()
}

describe('ChatView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    auth.refresh.mockResolvedValue(sessionAs('viewer'))
    bases.get.mockResolvedValue(ok(base))
    ai.profiles.mockResolvedValue(
      ok({
        defaultChatProfile: 'local',
        chatProfiles: [
          {
            name: 'local',
            provider: 'ollama',
            model: 'qwen2.5:7b',
            selectable: true,
            available: true,
          },
        ],
        embedding: { provider: 'ollama', model: 'bge-m3', dimensions: 1024 },
      }),
    )
    conversations.list.mockResolvedValue(
      ok({ items: [], page: 1, pageSize: 50, totalItems: 0, totalPages: 0 }),
    )
    conversations.start.mockResolvedValue(
      ok(
        { id: 'conv-1', knowledgeBaseId: 'kb-1', title: '', createdAt: '2026-11-20T10:00:00Z' },
        201,
      ),
    )
    conversations.stream.mockImplementation(streamingAnswer())
    conversations.get.mockResolvedValue(ok(conversation()))
  })

  it('invites to ask about the documents of the base', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1/chat' })

    expect(wrapper.text()).toContain('Pergunte algo sobre os documentos de “Manuais”')
  })

  it('starts a conversation with the first question and shows the streamed answer', async () => {
    conversations.list
      .mockResolvedValueOnce(ok({ items: [], page: 1, pageSize: 50, totalItems: 0, totalPages: 0 }))
      .mockResolvedValueOnce(
        ok({
          items: [
            {
              id: 'conv-1',
              knowledgeBaseId: 'kb-1',
              title: 'O que fazer em vazamentos?',
              messageCount: 2,
              createdAt: '2026-11-20T10:00:00Z',
              updatedAt: '2026-11-20T10:01:00Z',
            },
          ],
          page: 1,
          pageSize: 50,
          totalItems: 1,
          totalPages: 1,
        }),
      )
    const { wrapper, router } = await mountWithApp(App, { path: '/knowledge-bases/kb-1/chat' })

    await ask(wrapper, ' O que fazer em vazamentos? ')

    expect(conversations.start).toHaveBeenCalledWith('kb-1')
    expect(router.currentRoute.value.fullPath).toBe('/knowledge-bases/kb-1/chat/conv-1')
    expect(conversations.stream).toHaveBeenCalledWith(
      'conv-1',
      { question: 'O que fazer em vazamentos?', profile: null },
      expect.any(Object),
      expect.any(AbortSignal),
    )
    expect(wrapper.text()).toContain('O que fazer em vazamentos?')
    expect(wrapper.text()).toContain('Isole a área num raio de 50 metros [1].')
    expect(wrapper.text()).toContain('qwen2.5:7b · 2,5 s')
    expect(conversations.list).toHaveBeenCalledTimes(2)
    expect(wrapper.find('h1').text()).toBe('O que fazer em vazamentos?')
    expect((wrapper.find('textarea').element as HTMLTextAreaElement).value).toBe('')
  })

  it('gives the question back when the server refuses it', async () => {
    conversations.stream.mockResolvedValueOnce({
      success: false,
      data: null,
      notifications: [
        { key: 'request.rateLimited', message: 'Too many requests.', type: 'businessRule' },
      ],
      status: 429,
    })
    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1/chat/conv-1' })

    await ask(wrapper, 'O que fazer em vazamentos?')

    expect(wrapper.find('[role="log"]').text()).not.toContain('O que fazer em vazamentos?')
    expect((wrapper.find('textarea').element as HTMLTextAreaElement).value).toBe(
      'O que fazer em vazamentos?',
    )
  })

  it('stops the answer and gives the question back', async () => {
    conversations.get.mockResolvedValue(ok(conversation()))
    conversations.stream.mockImplementationOnce(
      (_id, _request, handlers, signal) =>
        new Promise((_resolve, reject) => {
          handlers.delta('Isole')
          signal.addEventListener('abort', () => reject(new DOMException('Stopped', 'AbortError')))
        }),
    )
    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1/chat/conv-1' })
    await ask(wrapper, 'O que fazer em vazamentos?')
    expect(wrapper.text()).toContain('Isole')

    buttonByText(wrapper.element, 'Parar')!.click()
    await flushPromises()

    expect(wrapper.find('[role="log"]').text()).not.toContain('Isole')
    expect((wrapper.find('textarea').element as HTMLTextAreaElement).value).toBe(
      'O que fazer em vazamentos?',
    )
  })

  it('shows the history and warns about answers without sources', async () => {
    conversations.get.mockResolvedValue(
      ok(
        conversation([
          message({
            id: 'q1',
            role: 'user',
            content: 'Qual a política de férias?',
            provider: null,
            model: null,
          }),
          message({ id: 'a1', content: 'Os documentos não contêm essa informação.' }),
          message({
            id: 'q2',
            role: 'user',
            content: 'E o horário do refeitório?',
            provider: null,
            model: null,
          }),
          message({
            id: 'a2',
            content: 'Não encontrei essa informação nos documentos desta base de conhecimento.',
            provider: 'lexora',
            model: 'no-context',
          }),
        ]),
      ),
    )

    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1/chat/conv-1' })

    expect(wrapper.find('h1').text()).toBe('Vazamentos')
    expect(wrapper.text()).toContain('Esta resposta não tem fonte nos documentos da base.')
    expect(wrapper.text()).toContain('Resposta automática')
  })

  it('leaves a conversation that cannot be opened', async () => {
    conversations.get.mockResolvedValue({
      success: false,
      data: null,
      notifications: [
        { key: 'conversation.notFound', message: 'Conversation not found.', type: 'notFound' },
      ],
      status: 404,
    })

    const { router } = await mountWithApp(App, { path: '/knowledge-bases/kb-1/chat/other' })

    await vi.waitFor(() =>
      expect(router.currentRoute.value.fullPath).toBe('/knowledge-bases/kb-1/chat'),
    )
  })

  it('sends with Enter and breaks the line with Shift+Enter', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/knowledge-bases/kb-1/chat' })
    const input = wrapper.find('textarea')
    await input.setValue('Pergunta')

    await input.trigger('keydown', { key: 'Enter', shiftKey: true })
    expect(conversations.start).not.toHaveBeenCalled()

    await input.trigger('keydown', { key: 'Enter' })
    await flushPromises()
    expect(conversations.start).toHaveBeenCalledTimes(1)
  })

  it('opens the cited excerpt and downloads its document', async () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined)
    documents.download.mockResolvedValueOnce(
      ok({
        url: 'http://localhost:8333/lexora/doc-1?signature=x',
        expiresAt: '2026-11-20T10:05:00Z',
      }),
    )
    conversations.get.mockResolvedValue(
      ok(
        conversation([
          message({ id: 'q1', role: 'user', content: 'O que fazer?', provider: null, model: null }),
          message({
            id: 'a1',
            content: 'Isole a área num raio de 50 metros [1].',
            citations: [
              {
                index: 1,
                documentId: 'doc-1',
                documentName: 'manual-seguranca.pdf',
                pageNumber: 1,
                sheetName: null,
                excerpt: 'Vazamentos químicos: isole a área num raio de 50 metros.',
                score: 0.03,
              },
            ],
          }),
        ]),
      ),
    )
    const { wrapper } = await mountWithApp(App, {
      path: '/knowledge-bases/kb-1/chat/conv-1',
      attachTo: document.body,
    })

    await wrapper.find('button[aria-label="Abrir a fonte 1"]').trigger('click')
    await flushPromises()

    expect(document.body.textContent).toContain('Fonte 1')
    expect(document.body.textContent).toContain(
      'Vazamentos químicos: isole a área num raio de 50 metros.',
    )
    buttonByText(document.body, 'Baixar o documento original')!.click()
    await flushPromises()
    expect(documents.download).toHaveBeenCalledWith('doc-1')
    expect((click.mock.contexts[0] as HTMLAnchorElement).href).toBe(
      'http://localhost:8333/lexora/doc-1?signature=x',
    )
  })
})
