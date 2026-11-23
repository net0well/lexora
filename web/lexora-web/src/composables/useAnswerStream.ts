import { onScopeDispose, readonly, ref, shallowRef } from 'vue'

import { isAbort } from '@/api/http'
import type { ApiNotification, ApiResult } from '@/api/types/commandResult'
import type {
  AnswerDone,
  AnswerStreamHandlers,
  Citation,
  ConversationMessage,
  QuestionRequest,
} from '@/api/types/conversation'

export interface AnswerDraft {
  text: string
  citations: Citation[]
  grounded: boolean | null
}

export type AnswerOutcome =
  | { kind: 'answered'; message: ConversationMessage }
  | { kind: 'refused'; notifications: ApiNotification[] }
  | { kind: 'stopped' }

export interface UseAnswerStreamOptions {
  stream: (
    conversationId: string,
    request: QuestionRequest,
    handlers: AnswerStreamHandlers,
    signal: AbortSignal,
  ) => Promise<ApiResult<null>>
}

export function useAnswerStream({ stream }: UseAnswerStreamOptions) {
  const draft = ref<AnswerDraft | null>(null)
  const streaming = shallowRef(false)
  let controller: AbortController | null = null

  function stop(): void {
    controller?.abort()
  }

  // The draft stays until the caller has stored the final message, so the answer never blinks out.
  function clear(): void {
    if (!streaming.value) {
      draft.value = null
    }
  }

  async function ask(conversationId: string, request: QuestionRequest): Promise<AnswerOutcome> {
    stop()
    const current = new AbortController()
    controller = current
    draft.value = { text: '', citations: [], grounded: null }
    streaming.value = true
    const finished: { value: AnswerDone | null } = { value: null }

    try {
      const result = await stream(
        conversationId,
        request,
        {
          delta: (text) => {
            if (draft.value) {
              draft.value.text += text
            }
          },
          citations: ({ citations, grounded }) => {
            if (draft.value) {
              draft.value.citations = citations
              draft.value.grounded = grounded
            }
          },
          done: (done) => {
            finished.value = done
          },
        },
        current.signal,
      )
      const done = finished.value

      if (!result.success || !done || !draft.value) {
        return { kind: 'refused', notifications: result.notifications }
      }

      return {
        kind: 'answered',
        message: {
          id: done.messageId,
          role: 'assistant',
          content: draft.value.text,
          citations: draft.value.citations,
          provider: done.provider,
          model: done.model,
          inputTokens: done.inputTokens,
          outputTokens: done.outputTokens,
          latencyMs: done.latencyMs,
          createdAt: new Date().toISOString(),
        },
      }
    } catch (error) {
      if (isAbort(error)) {
        return { kind: 'stopped' }
      }

      throw error
    } finally {
      if (controller === current) {
        controller = null
        streaming.value = false
      }
    }
  }

  onScopeDispose(stop)

  return { draft: readonly(draft), streaming: readonly(streaming), ask, stop, clear }
}
