import { defineStore } from 'pinia'
import { computed, ref, shallowRef } from 'vue'

import { conversationsApi } from '@/api/conversations'
import type {
  AnswerStreamHandlers,
  Conversation,
  ConversationListItem,
  ConversationMessage,
  QuestionRequest,
} from '@/api/types/conversation'
import { resetWhenUserChanges } from './sessionScope'

const listSize = 50

export const useConversationStore = defineStore('conversation', () => {
  const knowledgeBaseId = shallowRef<string | null>(null)
  const conversations = shallowRef<ConversationListItem[]>([])
  const pendingLists = shallowRef(0)
  const loadingList = computed(() => pendingLists.value > 0)
  const current = ref<Conversation | null>(null)
  const pendingOpens = shallowRef(0)
  const loadingCurrent = computed(() => pendingOpens.value > 0)

  resetWhenUserChanges(() => {
    knowledgeBaseId.value = null
    conversations.value = []
    current.value = null
  })

  async function loadList(baseId: string, signal?: AbortSignal) {
    if (knowledgeBaseId.value !== baseId) {
      knowledgeBaseId.value = baseId
      conversations.value = []
    }

    pendingLists.value++

    try {
      const result = await conversationsApi.list(
        { knowledgeBaseId: baseId, page: 1, pageSize: listSize },
        signal,
      )

      if (result.success && result.data && knowledgeBaseId.value === baseId) {
        conversations.value = result.data.items
        const listed = result.data.items.find((item) => item.id === current.value?.id)

        if (current.value && listed && listed.title !== current.value.title) {
          current.value.title = listed.title
        }
      }

      return result
    } finally {
      pendingLists.value--
    }
  }

  async function open(id: string, signal?: AbortSignal) {
    if (current.value?.id !== id) {
      current.value = null
    }

    pendingOpens.value++

    try {
      const result = await conversationsApi.get(id, signal)
      current.value = result.success ? result.data : null

      return result
    } finally {
      pendingOpens.value--
    }
  }

  function close(): void {
    current.value = null
  }

  async function start(baseId: string) {
    const result = await conversationsApi.start(baseId)

    if (result.success && result.data) {
      const started = result.data
      current.value = { ...started, messages: [], updatedAt: started.createdAt }

      if (knowledgeBaseId.value === baseId) {
        conversations.value = [
          { ...started, messageCount: 0, updatedAt: started.createdAt },
          ...conversations.value,
        ]
      }
    }

    return result
  }

  function addMessage(conversationId: string, message: ConversationMessage): void {
    if (current.value?.id === conversationId) {
      current.value.messages.push(message)
    }
  }

  function removeMessage(conversationId: string, messageId: string): void {
    if (current.value?.id === conversationId) {
      current.value.messages = current.value.messages.filter((message) => message.id !== messageId)
    }
  }

  async function remove(id: string) {
    const result = await conversationsApi.delete(id)

    if (result.success) {
      conversations.value = conversations.value.filter((conversation) => conversation.id !== id)

      if (current.value?.id === id) {
        current.value = null
      }
    }

    return result
  }

  function stream(
    conversationId: string,
    request: QuestionRequest,
    handlers: AnswerStreamHandlers,
    signal: AbortSignal,
  ) {
    return conversationsApi.stream(conversationId, request, handlers, signal)
  }

  return {
    knowledgeBaseId,
    conversations,
    pendingLists,
    loadingList,
    current,
    pendingOpens,
    loadingCurrent,
    loadList,
    open,
    close,
    start,
    addMessage,
    removeMessage,
    remove,
    stream,
  }
})
