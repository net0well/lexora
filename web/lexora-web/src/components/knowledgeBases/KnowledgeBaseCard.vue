<script setup lang="ts">
import Tag from 'primevue/tag'
import { useI18n } from 'vue-i18n'

import type { KnowledgeBaseListItem } from '@/api/types/knowledgeBase'
import { useFormat } from '@/composables/useFormat'

defineProps<{ item: KnowledgeBaseListItem }>()

const { t } = useI18n()
const format = useFormat()
</script>

<template>
  <RouterLink :to="{ name: 'knowledge-base', params: { id: item.id } }" class="kb-card">
    <div class="kb-card__title">
      <i class="pi pi-book" aria-hidden="true" />
      <h2>{{ item.name }}</h2>
      <Tag
        v-if="item.status === 'archived'"
        :value="t('knowledgeBases.archived')"
        severity="secondary"
      />
    </div>
    <p class="kb-card__description">
      {{ item.description || t('knowledgeBases.noDescription') }}
    </p>
    <footer class="kb-card__meta">
      <span>{{
        t('knowledgeBases.documentCount', { count: item.documentCount }, item.documentCount)
      }}</span>
      <span>{{ t('knowledgeBases.updatedAt', { date: format.dateTime(item.updatedAt) }) }}</span>
    </footer>
  </RouterLink>
</template>

<style scoped>
.kb-card {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  min-height: 9.5rem;
  padding: 1.125rem 1.25rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 12px;
  background: var(--p-content-background);
  color: inherit;
  text-decoration: none;
  transition:
    border-color 0.15s,
    box-shadow 0.15s;
}

.kb-card:hover,
.kb-card:focus-visible {
  border-color: var(--p-primary-300);
  box-shadow: 0 2px 8px rgb(0 0 0 / 6%);
}

.kb-card__title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.kb-card__title i {
  color: var(--p-primary-600);
}

.kb-card__title h2 {
  flex: 1;
  margin: 0;
  overflow: hidden;
  font-size: 1.05rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.kb-card__description {
  display: -webkit-box;
  flex: 1;
  margin: 0;
  overflow: hidden;
  color: var(--p-text-muted-color);
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 3;
  line-clamp: 3;
}

.kb-card__meta {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
  color: var(--p-text-muted-color);
  font-size: 0.8rem;
}
</style>
