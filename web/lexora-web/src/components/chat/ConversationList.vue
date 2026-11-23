<script setup lang="ts">
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import { useConfirm } from 'primevue/useconfirm'
import { storeToRefs } from 'pinia'
import { watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import { isAbort } from '@/api/http'
import type { ConversationListItem } from '@/api/types/conversation'
import { useFormat } from '@/composables/useFormat'
import { useNotifications } from '@/composables/useNotifications'
import { useConversationStore } from '@/stores/conversation'

const { knowledgeBaseId, activeId = null } = defineProps<{
  knowledgeBaseId: string
  activeId?: string | null
}>()

const { t } = useI18n()
const router = useRouter()
const confirm = useConfirm()
const format = useFormat()
const store = useConversationStore()
const { conversations, loadingList } = storeToRefs(store)
const { notify, success } = useNotifications()

function confirmDelete(conversation: ConversationListItem): void {
  confirm.require({
    header: t('chat.list.deleteTitle'),
    message: t('chat.list.deleteConfirm', { title: conversation.title }),
    icon: 'pi pi-trash',
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    acceptProps: { label: t('common.delete'), severity: 'danger' },
    accept: async () => {
      const result = await store.remove(conversation.id)

      if (!result.success) {
        notify(result.notifications)
        return
      }

      success(t('chat.list.deleted'))

      if (conversation.id === activeId) {
        await router.push({ name: 'chat', params: { id: knowledgeBaseId } })
      }
    },
  })
}

watch(
  () => knowledgeBaseId,
  async (id, _previous, onCleanup) => {
    const controller = new AbortController()
    onCleanup(() => controller.abort())

    try {
      const result = await store.loadList(id, controller.signal)

      if (!result.success) {
        notify(result.notifications)
      }
    } catch (error) {
      if (!isAbort(error)) {
        throw error
      }
    }
  },
  { immediate: true },
)
</script>

<template>
  <nav class="conversations" :aria-label="t('chat.list.label')">
    <RouterLink
      v-slot="{ navigate }"
      :to="{ name: 'chat', params: { id: knowledgeBaseId } }"
      custom
    >
      <Button :label="t('chat.list.new')" icon="pi pi-plus" outlined fluid @click="navigate" />
    </RouterLink>

    <div v-if="loadingList && conversations.length === 0" class="conversations__loading">
      <Skeleton v-for="index in 4" :key="index" height="2.75rem" />
    </div>

    <p v-else-if="conversations.length === 0" class="conversations__empty">
      {{ t('chat.list.empty') }}
    </p>

    <ul v-else class="conversations__items">
      <li
        v-for="conversation in conversations"
        :key="conversation.id"
        class="conversations__item"
        :class="{ 'conversations__item--active': conversation.id === activeId }"
      >
        <RouterLink
          :to="{ name: 'chat', params: { id: knowledgeBaseId, conversationId: conversation.id } }"
          class="conversations__link"
          :aria-current="conversation.id === activeId ? 'page' : undefined"
        >
          <span class="conversations__title">{{
            conversation.title || t('chat.list.untitled')
          }}</span>
          <small class="conversations__date">{{ format.dateTime(conversation.updatedAt) }}</small>
        </RouterLink>
        <Button
          icon="pi pi-trash"
          text
          rounded
          size="small"
          severity="secondary"
          :aria-label="t('chat.list.delete', { title: conversation.title })"
          @click="confirmDelete(conversation)"
        />
      </li>
    </ul>
  </nav>
</template>

<style scoped>
.conversations {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  overflow-y: auto;
  padding: 1rem 0.75rem;
  border-right: 1px solid var(--p-content-border-color);
  background: var(--p-content-background);
}

.conversations__loading {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.conversations__empty {
  margin: 0;
  padding: 0 0.25rem;
  color: var(--p-text-muted-color);
  font-size: 0.875rem;
}

.conversations__items {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.conversations__item {
  display: flex;
  align-items: center;
  border-radius: 8px;
}

.conversations__item:hover,
.conversations__item--active {
  background: var(--p-primary-50);
}

.conversations__link {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-width: 0;
  padding: 0.5rem 0.625rem;
  color: inherit;
  text-decoration: none;
}

.conversations__title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.conversations__date {
  color: var(--p-text-muted-color);
}
</style>
