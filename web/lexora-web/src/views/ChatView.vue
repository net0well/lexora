<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { computed, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import { isAbort } from '@/api/http'
import type { Citation, ConversationMessage } from '@/api/types/conversation'
import ChatComposer from '@/components/chat/ChatComposer.vue'
import ChatThread from '@/components/chat/ChatThread.vue'
import CitationPanel from '@/components/chat/CitationPanel.vue'
import ConversationList from '@/components/chat/ConversationList.vue'
import { useAnswerStream } from '@/composables/useAnswerStream'
import { useNotifications } from '@/composables/useNotifications'
import { useConversationStore } from '@/stores/conversation'
import { useKnowledgeBasesStore } from '@/stores/knowledgeBases'

const { id, conversationId = '' } = defineProps<{ id: string; conversationId?: string }>()

const { t } = useI18n()
const router = useRouter()
const bases = useKnowledgeBasesStore()
const { current: base } = storeToRefs(bases)
const conversations = useConversationStore()
const { current: conversation } = storeToRefs(conversations)
const { notify } = useNotifications()
const answers = useAnswerStream({
  stream: (target, request, handlers, signal) =>
    conversations.stream(target, request, handlers, signal),
})
const { draft, streaming } = answers

const question = shallowRef('')
const profile = shallowRef<string | null>(null)
const openCitations = shallowRef<readonly Citation[]>([])
const openCitation = shallowRef<number | null>(null)

function showCitation(citations: readonly Citation[], index: number): void {
  openCitations.value = citations
  openCitation.value = index
}

const messages = computed(() =>
  conversation.value && conversation.value.id === conversationId ? conversation.value.messages : [],
)

async function ignoreAbort(task: Promise<unknown>): Promise<void> {
  try {
    await task
  } catch (error) {
    if (!isAbort(error)) {
      throw error
    }
  }
}

function pendingQuestion(text: string): ConversationMessage {
  return {
    id: `pending-${Date.now()}`,
    role: 'user',
    content: text,
    citations: [],
    provider: null,
    model: null,
    inputTokens: 0,
    outputTokens: 0,
    latencyMs: null,
    createdAt: new Date().toISOString(),
  }
}

async function targetConversation(): Promise<string | null> {
  if (conversationId) {
    return conversationId
  }

  const started = await conversations.start(id)

  if (!started.success || !started.data) {
    notify(started.notifications)
    return null
  }

  await router.replace({ name: 'chat', params: { id, conversationId: started.data.id } })

  return started.data.id
}

async function send(text: string): Promise<void> {
  const target = await targetConversation()

  if (!target) {
    return
  }

  const pending = pendingQuestion(text)
  conversations.addMessage(target, pending)
  question.value = ''

  const outcome = await answers.ask(target, { question: text, profile: profile.value })

  if (outcome.kind === 'answered') {
    conversations.addMessage(target, outcome.message)
    answers.clear()
    void ignoreAbort(conversations.loadList(id))
    return
  }

  conversations.removeMessage(target, pending.id)
  answers.clear()

  if (!question.value) {
    question.value = text
  }

  if (outcome.kind === 'refused') {
    notify(outcome.notifications)
  }
}

watch(
  () => id,
  (current, _previous, onCleanup) => {
    if (base.value?.id === current) {
      return
    }

    const controller = new AbortController()
    onCleanup(() => controller.abort())
    void ignoreAbort(bases.open(current, controller.signal))
  },
  { immediate: true },
)

watch(
  () => conversationId,
  (current, _previous, onCleanup) => {
    answers.stop()

    if (!current) {
      conversations.close()
      return
    }

    if (conversation.value?.id === current) {
      return
    }

    const controller = new AbortController()
    onCleanup(() => controller.abort())
    void ignoreAbort(
      conversations.open(current, controller.signal).then(async (result) => {
        if (!result.success) {
          notify(result.notifications)
          await router.replace({ name: 'chat', params: { id } })
        }
      }),
    )
  },
  { immediate: true },
)
</script>

<template>
  <section class="chat">
    <ConversationList :knowledge-base-id="id" :active-id="conversationId || null" />

    <div class="chat__main">
      <header class="chat__header">
        <RouterLink :to="{ name: 'knowledge-base', params: { id } }" class="back-link chat__back">
          <i class="pi pi-arrow-left" aria-hidden="true" />
          {{ base?.name ?? t('nav.knowledgeBases') }}
        </RouterLink>
        <h1 class="chat__title">{{ conversation?.title || t('chat.newConversation') }}</h1>
      </header>

      <ChatThread
        class="chat__thread"
        :messages="messages"
        :draft="draft"
        :streaming="streaming"
        :knowledge-base-name="base?.name ?? ''"
        @cite="showCitation"
      />

      <ChatComposer
        v-model="question"
        v-model:profile="profile"
        :streaming="streaming"
        @send="send"
        @stop="answers.stop()"
      />
    </div>

    <CitationPanel v-model:index="openCitation" :citations="openCitations" />
  </section>
</template>

<style scoped>
.chat {
  display: grid;
  grid-template-columns: 17rem minmax(0, 1fr);
  height: calc(100vh - 3.5rem);
}

.chat__main {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.chat__header {
  padding: 0.875rem 1.5rem 0.75rem;
  border-bottom: 1px solid var(--p-content-border-color);
  background: var(--p-content-background);
}

.chat__back {
  margin-bottom: 0.25rem;
}

.chat__title {
  margin: 0;
  overflow: hidden;
  font-size: 1.1rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.chat__thread {
  flex: 1;
  min-height: 0;
}

@media (max-width: 768px) {
  .chat {
    grid-template-columns: 1fr;
    grid-template-rows: auto minmax(0, 1fr);
  }

  .chat :deep(.conversations) {
    max-height: 30vh;
    border-right: none;
    border-bottom: 1px solid var(--p-content-border-color);
  }
}
</style>
