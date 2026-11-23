<script setup lang="ts">
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { conversationRules } from '@/api/types/conversation'
import ProfileSelector from './ProfileSelector.vue'

const question = defineModel<string>({ required: true })
const profile = defineModel<string | null>('profile', { default: null })

const { streaming = false, disabled = false } = defineProps<{
  streaming?: boolean
  disabled?: boolean
}>()

const emit = defineEmits<{ send: [question: string]; stop: [] }>()

const counterFrom = conversationRules.questionMaxLength - 500

const { t } = useI18n()

const text = computed(() => question.value.trim())
const canSend = computed(() => text.value.length > 0 && !streaming && !disabled)

function send(): void {
  if (canSend.value) {
    emit('send', text.value)
  }
}

function keydown(event: KeyboardEvent): void {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
    event.preventDefault()
    send()
  }
}
</script>

<template>
  <form class="composer" @submit.prevent="send">
    <Textarea
      v-model="question"
      class="composer__input"
      rows="1"
      auto-resize
      :maxlength="conversationRules.questionMaxLength"
      :placeholder="t('chat.placeholder')"
      :aria-label="t('chat.placeholder')"
      :disabled="disabled"
      @keydown="keydown"
    />
    <div class="composer__bar">
      <ProfileSelector v-model="profile" />
      <small v-if="question.length > counterFrom" class="composer__counter">
        {{ question.length }}/{{ conversationRules.questionMaxLength }}
      </small>
      <span class="composer__hint">{{ t('chat.sendHint') }}</span>
      <Button
        v-if="streaming"
        type="button"
        :label="t('chat.stop')"
        icon="pi pi-stop-circle"
        severity="secondary"
        @click="emit('stop')"
      />
      <Button v-else type="submit" :label="t('chat.send')" icon="pi pi-send" :disabled="!canSend" />
    </div>
  </form>
</template>

<style scoped>
.composer {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.875rem 1.25rem 1.125rem;
  border-top: 1px solid var(--p-content-border-color);
  background: var(--p-content-background);
}

.composer__input {
  width: 100%;
  max-height: 12rem;
}

.composer__bar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.composer__counter,
.composer__hint {
  color: var(--p-text-muted-color);
  font-size: 0.8rem;
}

.composer__hint {
  flex: 1;
  text-align: right;
}
</style>
