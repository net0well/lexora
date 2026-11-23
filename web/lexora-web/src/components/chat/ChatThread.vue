<script setup lang="ts">
import { shallowRef, useTemplateRef, watch, type DeepReadonly } from 'vue'
import { useI18n } from 'vue-i18n'

import type { ConversationMessage } from '@/api/types/conversation'
import type { AnswerDraft } from '@/composables/useAnswerStream'
import MessageBubble from './MessageBubble.vue'

const {
  messages,
  draft = null,
  streaming = false,
  knowledgeBaseName = '',
} = defineProps<{
  messages: readonly ConversationMessage[]
  draft?: DeepReadonly<AnswerDraft> | null
  streaming?: boolean
  knowledgeBaseName?: string
}>()

const stickToEndDistance = 80

const { t } = useI18n()
const container = useTemplateRef<HTMLElement>('container')
const following = shallowRef(true)

function scrolled(): void {
  const element = container.value

  if (element) {
    following.value =
      element.scrollHeight - element.scrollTop - element.clientHeight < stickToEndDistance
  }
}

watch(
  () => messages.length,
  () => {
    following.value = true
  },
)

watch(
  () => [messages.length, draft?.text.length ?? 0, draft?.citations.length ?? 0],
  () => {
    const element = container.value

    if (element && following.value) {
      element.scrollTop = element.scrollHeight
    }
  },
  { flush: 'post' },
)
</script>

<template>
  <div ref="container" class="thread" role="log" aria-live="polite" @scroll.passive="scrolled">
    <div v-if="messages.length === 0 && !draft" class="thread__empty">
      <i class="pi pi-comments thread__icon" aria-hidden="true" />
      <p>{{ t('chat.empty', { name: knowledgeBaseName }) }}</p>
    </div>

    <MessageBubble
      v-for="message in messages"
      :key="message.id"
      :role="message.role"
      :content="message.content"
      :citations="message.citations"
      :provider="message.provider"
      :model="message.model"
      :latency-ms="message.latencyMs"
    />

    <MessageBubble
      v-if="draft"
      role="assistant"
      :content="draft.text"
      :citations="draft.citations"
      :grounded="draft.grounded"
      :streaming="streaming"
    />
  </div>
</template>

<style scoped>
.thread {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
  overflow-y: auto;
  padding: 1.25rem 1.5rem;
}

.thread__empty {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  color: var(--p-text-muted-color);
  text-align: center;
}

.thread__icon {
  font-size: 2rem;
}
</style>
