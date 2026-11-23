import { flushPromises } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

import type { ApiResult } from '@/api/types/commandResult'
import type { AnswerStreamHandlers, QuestionRequest } from '@/api/types/conversation'
import { withSetup } from '@/__tests__/support'
import { ok } from '@/__tests__/tokens'
import { useAnswerStream } from '../useAnswerStream'

type Stream = (
  conversationId: string,
  request: QuestionRequest,
  handlers: AnswerStreamHandlers,
  signal: AbortSignal,
) => Promise<ApiResult<null>>

const question: QuestionRequest = { question: 'O que fazer?', profile: null }

const done = {
  conversationId: 'conv-1',
  messageId: 'answer-1',
  provider: 'ollama',
  model: 'qwen2.5:7b',
  inputTokens: 300,
  outputTokens: 20,
  latencyMs: 2500,
}

function controlled() {
  let handlers!: AnswerStreamHandlers
  let finish!: (result: ApiResult<null>) => void
  let signal!: AbortSignal
  const stream = vi.fn<Stream>(
    (_id, _request, received, receivedSignal) =>
      new Promise((resolve, reject) => {
        handlers = received
        signal = receivedSignal
        finish = resolve
        receivedSignal.addEventListener('abort', () =>
          reject(new DOMException('Stopped', 'AbortError')),
        )
      }),
  )

  return {
    stream,
    handlers: () => handlers,
    signal: () => signal,
    finish: (result: ApiResult<null>) => finish(result),
  }
}

describe('useAnswerStream', () => {
  it('builds the draft from delta, citations and done', async () => {
    const source = controlled()
    const { result: answers } = withSetup(() => useAnswerStream({ stream: source.stream }))

    const outcome = answers.ask('conv-1', question)
    expect(answers.streaming.value).toBe(true)
    source.handlers().delta('Isole ')
    source.handlers().delta('a área [1].')
    expect(answers.draft.value?.text).toBe('Isole a área [1].')
    source.handlers().citations({
      citations: [
        {
          index: 1,
          documentId: 'doc-1',
          documentName: 'manual.pdf',
          pageNumber: 1,
          sheetName: null,
          excerpt: 'Isole a área.',
          score: 0.03,
        },
      ],
      grounded: true,
    })
    source.handlers().done(done)
    source.finish(ok(null))

    const answered = await outcome

    expect(answered).toMatchObject({
      kind: 'answered',
      message: {
        id: 'answer-1',
        role: 'assistant',
        content: 'Isole a área [1].',
        model: 'qwen2.5:7b',
        latencyMs: 2500,
      },
    })
    expect(answers.streaming.value).toBe(false)
    expect(answers.draft.value?.grounded).toBe(true)

    answers.clear()
    expect(answers.draft.value).toBeNull()
  })

  it('reports a refused question', async () => {
    const refused = { key: 'ai.unavailable', message: 'Down.', type: 'unavailable' as const }
    const { result: answers } = withSetup(() =>
      useAnswerStream({
        stream: async () => ({ success: false, data: null, notifications: [refused], status: 503 }),
      }),
    )

    await expect(answers.ask('conv-1', question)).resolves.toEqual({
      kind: 'refused',
      notifications: [refused],
    })
  })

  it('stops the answer on request', async () => {
    const source = controlled()
    const { result: answers } = withSetup(() => useAnswerStream({ stream: source.stream }))

    const outcome = answers.ask('conv-1', question)
    answers.stop()

    await expect(outcome).resolves.toEqual({ kind: 'stopped' })
    expect(answers.streaming.value).toBe(false)
  })

  it('stops the previous answer when a new question starts', async () => {
    const source = controlled()
    const { result: answers } = withSetup(() => useAnswerStream({ stream: source.stream }))

    const first = answers.ask('conv-1', question)
    const firstSignal = source.signal()
    void answers.ask('conv-1', { question: 'E depois?', profile: null })

    await expect(first).resolves.toEqual({ kind: 'stopped' })
    expect(firstSignal.aborted).toBe(true)
    expect(answers.streaming.value).toBe(true)
  })

  it('stops the answer when its owner goes away', async () => {
    const source = controlled()
    const { result: answers, unmount } = withSetup(() => useAnswerStream({ stream: source.stream }))
    void answers.ask('conv-1', question)
    await flushPromises()

    unmount()

    expect(source.signal().aborted).toBe(true)
  })
})
