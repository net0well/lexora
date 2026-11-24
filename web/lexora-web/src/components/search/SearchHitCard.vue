<script setup lang="ts">
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import { computed, shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'

import type { SearchHit } from '@/api/types/search'

const { hit, rank } = defineProps<{ hit: SearchHit; rank: number }>()

const emit = defineEmits<{ download: [documentId: string] }>()

const longExcerpt = 420

const { t } = useI18n()
const expanded = shallowRef(false)

const location = computed(() =>
  [
    hit.pageNumber !== null ? t('search.page', { page: hit.pageNumber }) : null,
    hit.sheetName ? t('search.sheet', { sheet: hit.sheetName }) : null,
    hit.sectionTitle,
  ]
    .filter(Boolean)
    .join(' · '),
)

const long = computed(() => hit.content.length > longExcerpt)
</script>

<template>
  <article class="hit">
    <header class="hit__header">
      <span class="hit__rank" aria-hidden="true">{{ rank }}</span>
      <div class="hit__source">
        <strong class="hit__document">{{ hit.documentName }}</strong>
        <span v-if="location" class="hit__location">{{ location }}</span>
      </div>
      <Tag
        :value="t(`search.match.${hit.matchedBy}`)"
        severity="secondary"
        :title="t('search.score', { score: hit.score.toFixed(4) })"
      />
      <Button
        icon="pi pi-download"
        text
        rounded
        severity="secondary"
        :aria-label="t('documents.actions.download')"
        @click="emit('download', hit.documentId)"
      />
    </header>
    <p class="hit__content" :class="{ 'hit__content--clamped': long && !expanded }">
      {{ hit.content }}
    </p>
    <Button
      v-if="long"
      :label="t(expanded ? 'search.showLess' : 'search.showMore')"
      text
      size="small"
      :aria-expanded="expanded"
      @click="expanded = !expanded"
    />
  </article>
</template>

<style scoped>
.hit {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 1rem 1.125rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 12px;
  background: var(--p-content-background);
}

.hit__header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.hit__rank {
  display: grid;
  flex: none;
  place-items: center;
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 999px;
  background: var(--p-primary-50);
  color: var(--p-primary-700);
  font-weight: 600;
}

.hit__source {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-width: 0;
}

.hit__document {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.hit__location {
  color: var(--p-text-muted-color);
  font-size: 0.85rem;
}

.hit__content {
  margin: 0;
  white-space: pre-line;
}

.hit__content--clamped {
  display: -webkit-box;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 6;
  line-clamp: 6;
}
</style>
