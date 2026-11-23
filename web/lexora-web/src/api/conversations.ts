import { fetchEventSource } from '@microsoft/fetch-event-source'

import { apiUrl, failure, http, networkUnavailable, readResult, withSessionRetry } from './http'
import type { ApiNotification, ApiResult, PagedResponse } from './types/commandResult'
import type {
  AnswerCitations,
  AnswerDone,
  AnswerStreamError,
  AnswerStreamHandlers,
  Conversation,
  ConversationListItem,
  QuestionRequest,
  StartedConversation,
} from './types/conversation'

export interface ConversationQuery {
  knowledgeBaseId?: string | null
  page?: number
  pageSize?: number
}

export const streamInterrupted: ApiNotification = {
  key: 'stream.interrupted',
  message: 'The answer stopped before it was complete.',
  type: 'unavailable',
}

class StreamRefused extends Error {}

function aborted(): DOMException {
  return new DOMException('The answer was stopped.', 'AbortError')
}

async function openStream(
  conversationId: string,
  request: QuestionRequest,
  handlers: AnswerStreamHandlers,
  signal: AbortSignal,
  token: string | null,
): Promise<ApiResult<null>> {
  let refusal: ApiResult<null> | null = null
  let streamError: ApiNotification[] | null = null
  let finished = false

  try {
    await fetchEventSource(apiUrl(`/conversations/${conversationId}/messages/stream`), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'text/event-stream',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(request),
      signal,
      // Hiding the tab would otherwise close the stream and post the question again.
      openWhenHidden: true,
      fetch: (input, init) => fetch(input, init),
      async onopen(response) {
        if (response.ok && response.headers.get('content-type')?.startsWith('text/event-stream')) {
          return
        }

        refusal = await readResult<null>(response)
        throw new StreamRefused()
      },
      onmessage(message) {
        const data: unknown = JSON.parse(message.data)

        switch (message.event) {
          case 'delta':
            handlers.delta((data as { text: string }).text)
            break
          case 'citations':
            handlers.citations(data as AnswerCitations)
            break
          case 'done':
            finished = true
            handlers.done(data as AnswerDone)
            break
          case 'error':
            streamError = (data as AnswerStreamError).notifications
            break
        }
      },
      // Never retry: a new request would ask the same question again and record it twice.
      onerror(error) {
        throw error
      },
    })
  } catch {
    if (signal.aborted) {
      throw aborted()
    }

    return refusal ?? failure(0, networkUnavailable)
  }

  if (signal.aborted) {
    throw aborted()
  }

  if (streamError) {
    return { success: false, data: null, notifications: streamError, status: 200 }
  }

  return finished
    ? { success: true, data: null, notifications: [], status: 200 }
    : failure(200, streamInterrupted)
}

export const conversationsApi = {
  list: (query: ConversationQuery = {}, signal?: AbortSignal) =>
    http.get<PagedResponse<ConversationListItem>>('/conversations', {
      query: { ...query },
      signal,
    }),
  get: (id: string, signal?: AbortSignal) =>
    http.get<Conversation>(`/conversations/${id}`, { signal }),
  start: (knowledgeBaseId: string) =>
    http.post<StartedConversation>('/conversations', { knowledgeBaseId }),
  delete: (id: string) => http.delete<null>(`/conversations/${id}`),
  stream: (
    conversationId: string,
    request: QuestionRequest,
    handlers: AnswerStreamHandlers,
    signal: AbortSignal,
  ) => withSessionRetry((token) => openStream(conversationId, request, handlers, signal, token)),
}
