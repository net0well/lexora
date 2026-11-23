import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'

import { conversationsApi } from '../conversations'
import { bindSession, type SessionBridge } from '../session'
import type { AnswerStreamHandlers } from '../types/conversation'

const encoder = new TextEncoder()

function sse(event: string, data: unknown): string {
  return `event: ${event}\ndata: ${JSON.stringify(data)}\n\n`
}

function eventStream(...chunks: string[]): Response {
  return new Response(chunks.join(''), {
    status: 200,
    headers: { 'Content-Type': 'text/event-stream' },
  })
}

function openStream(): { response: Response; push: (chunk: string) => void; end: () => void } {
  let controller!: ReadableStreamDefaultController<Uint8Array>
  const body = new ReadableStream<Uint8Array>({
    start(streamController) {
      controller = streamController
    },
  })

  return {
    response: new Response(body, { status: 200, headers: { 'Content-Type': 'text/event-stream' } }),
    push: (chunk) => controller.enqueue(encoder.encode(chunk)),
    end: () => controller.close(),
  }
}

function envelope(status: number, key: string): Response {
  return new Response(
    JSON.stringify({
      success: false,
      data: null,
      notifications: [{ key, message: key, type: 'businessRule' }],
    }),
    { status, headers: { 'Content-Type': 'application/json; charset=utf-8' } },
  )
}

const done = {
  conversationId: 'conv-1',
  messageId: 'answer-1',
  provider: 'ollama',
  model: 'qwen2.5:7b',
  inputTokens: 300,
  outputTokens: 20,
  latencyMs: 2500,
}

function recorder() {
  const events: string[] = []
  const handlers: AnswerStreamHandlers = {
    delta: (text) => events.push(`delta:${text}`),
    citations: ({ grounded }) => events.push(`citations:${grounded}`),
    done: ({ messageId }) => events.push(`done:${messageId}`),
  }

  return { events, handlers }
}

describe('conversationsApi.stream', () => {
  let fetchMock: Mock<typeof fetch>
  let token: string
  let session: SessionBridge

  beforeEach(() => {
    fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
    token = 'old-token'
    session = {
      accessToken: () => token,
      renew: vi.fn(async () => {
        token = 'new-token'
        return true
      }),
      expire: vi.fn(),
    }
    bindSession(session)
  })

  afterEach(() => {
    bindSession(null)
  })

  it('delivers the answer, the citations and the end in order', async () => {
    fetchMock.mockResolvedValueOnce(
      eventStream(
        sse('delta', { text: 'Isole ' }),
        sse('delta', { text: 'a área [1].' }),
        sse('citations', { citations: [], grounded: true }),
        sse('done', done),
      ),
    )
    const { events, handlers } = recorder()

    const result = await conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: 'local' },
      handlers,
      new AbortController().signal,
    )

    expect(result.success).toBe(true)
    expect(events).toEqual(['delta:Isole ', 'delta:a área [1].', 'citations:true', 'done:answer-1'])
    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/v1/conversations/conv-1/messages/stream')
    expect(init).toMatchObject({
      method: 'POST',
      body: '{"question":"O que fazer?","profile":"local"}',
    })
    expect(init?.headers).toMatchObject({
      Authorization: 'Bearer old-token',
      Accept: 'text/event-stream',
      'Content-Type': 'application/json',
    })
  })

  it('returns the reason of a refusal that came before the stream', async () => {
    fetchMock.mockResolvedValueOnce(envelope(429, 'request.rateLimited'))
    const { events, handlers } = recorder()

    const result = await conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: null },
      handlers,
      new AbortController().signal,
    )

    expect(result).toMatchObject({ success: false, status: 429 })
    expect(result.notifications[0]?.key).toBe('request.rateLimited')
    expect(events).toEqual([])
  })

  it('renews the session and asks once more after a 401', async () => {
    fetchMock
      .mockResolvedValueOnce(envelope(401, 'auth.unauthenticated'))
      .mockResolvedValueOnce(eventStream(sse('delta', { text: 'Isole.' }), sse('done', done)))
    const { handlers } = recorder()

    const result = await conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: null },
      handlers,
      new AbortController().signal,
    )

    expect(result.success).toBe(true)
    expect(session.renew).toHaveBeenCalledTimes(1)
    expect(
      fetchMock.mock.calls.map(
        ([, init]) => (init?.headers as Record<string, string>).Authorization,
      ),
    ).toEqual(['Bearer old-token', 'Bearer new-token'])
  })

  it('reports an error sent in the middle of the answer', async () => {
    fetchMock.mockResolvedValueOnce(
      eventStream(
        sse('delta', { text: 'Isole' }),
        sse('error', {
          notifications: [{ key: 'ai.unavailable', message: 'Down.', type: 'unavailable' }],
        }),
      ),
    )
    const { events, handlers } = recorder()

    const result = await conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: null },
      handlers,
      new AbortController().signal,
    )

    expect(events).toEqual(['delta:Isole'])
    expect(result).toMatchObject({ success: false })
    expect(result.notifications[0]?.key).toBe('ai.unavailable')
  })

  it('treats a stream that ends without its last event as interrupted', async () => {
    fetchMock.mockResolvedValueOnce(eventStream(sse('delta', { text: 'Isole' })))

    const result = await conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: null },
      recorder().handlers,
      new AbortController().signal,
    )

    expect(result.notifications[0]?.key).toBe('stream.interrupted')
  })

  it('never posts the question again after a connection failure', async () => {
    vi.useFakeTimers()
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    const pending = conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: null },
      recorder().handlers,
      new AbortController().signal,
    )
    await vi.advanceTimersByTimeAsync(10_000)
    const result = await pending

    expect(result.notifications[0]?.key).toBe('network.unavailable')
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('fails as an abort when the user stops the answer', async () => {
    const stream = openStream()
    fetchMock.mockResolvedValueOnce(stream.response)
    const controller = new AbortController()
    const { events, handlers } = recorder()

    const pending = conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: null },
      handlers,
      controller.signal,
    )
    stream.push(sse('delta', { text: 'Isole' }))
    await vi.waitFor(() => expect(events).toEqual(['delta:Isole']))
    controller.abort()

    await expect(pending).rejects.toMatchObject({ name: 'AbortError' })
  })

  it('keeps the same request when the tab is hidden', async () => {
    const stream = openStream()
    fetchMock.mockResolvedValueOnce(stream.response)
    const { events, handlers } = recorder()
    const pending = conversationsApi.stream(
      'conv-1',
      { question: 'O que fazer?', profile: null },
      handlers,
      new AbortController().signal,
    )
    await vi.waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1))

    Object.defineProperty(document, 'hidden', { configurable: true, value: true })
    document.dispatchEvent(new Event('visibilitychange'))
    stream.push(sse('delta', { text: 'Isole.' }))
    stream.push(sse('done', done))
    stream.end()
    const result = await pending
    Object.defineProperty(document, 'hidden', { configurable: true, value: false })

    expect(result.success).toBe(true)
    expect(events).toEqual(['delta:Isole.', 'done:answer-1'])
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
