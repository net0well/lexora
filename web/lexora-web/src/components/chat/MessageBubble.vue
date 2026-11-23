<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { conversationRules, type Citation, type MessageRole } from '@/api/types/conversation'
import MarkdownContent from './MarkdownContent.vue'

const {
  role,
  content,
  citations = [],
  grounded = null,
  provider = null,
  model = null,
  latencyMs = null,
  streaming = false,
} = defineProps<{
  role: MessageRole
  content: string
  citations?: readonly Citation[]
  grounded?: boolean | null
  provider?: string | null
  model?: string | null
  latencyMs?: number | null
  streaming?: boolean
}>()

const { t, n } = useI18n()

const automatic = computed(() => provider === conversationRules.noContextProvider)
const withoutSources = computed(
  () => role === 'assistant' && !streaming && (grounded ?? citations.length > 0) === false,
)
const details = computed(() => {
  if (role !== 'assistant' || streaming || automatic.value || !model) {
    return null
  }

  return latencyMs === null
    ? model
    : t('chat.answerDetails', {
        model,
        seconds: n(latencyMs / 1000, { maximumFractionDigits: 1 }),
      })
})
</script>

<template>
  <article class="bubble" :class="`bubble--${role}`" :aria-busy="streaming">
    <p v-if="role === 'user'" class="bubble__question">{{ content }}</p>

    <template v-else>
      <p v-if="streaming && !content" class="bubble__thinking">
        <i class="pi pi-spin pi-spinner" aria-hidden="true" />
        {{ t('chat.thinking') }}
      </p>
      <MarkdownContent v-else :source="content" />

      <p v-if="withoutSources" class="bubble__warning">
        <i class="pi pi-info-circle" aria-hidden="true" />
        {{ t('chat.withoutSources') }}
      </p>

      <footer v-if="automatic || details" class="bubble__footer">
        {{ automatic ? t('chat.automatic') : details }}
      </footer>
    </template>
  </article>
</template>

<style scoped>
.bubble {
  max-width: min(48rem, 92%);
  padding: 0.75rem 1rem;
  border-radius: 14px;
}

.bubble--user {
  align-self: flex-end;
  border-bottom-right-radius: 4px;
  background: var(--p-primary-600);
  color: var(--p-primary-contrast-color);
}

.bubble--assistant {
  align-self: flex-start;
  border: 1px solid var(--p-content-border-color);
  border-bottom-left-radius: 4px;
  background: var(--p-content-background);
}

.bubble__question {
  margin: 0;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.bubble__thinking {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0;
  color: var(--p-text-muted-color);
}

.bubble__warning {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  margin: 0.625rem 0 0;
  color: var(--p-orange-700);
  font-size: 0.875rem;
}

.bubble__footer {
  margin-top: 0.5rem;
  color: var(--p-text-muted-color);
  font-size: 0.8rem;
}
</style>
