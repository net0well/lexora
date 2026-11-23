<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import type { Citation } from '@/api/types/conversation'

const { citation, active = false } = defineProps<{ citation: Citation; active?: boolean }>()

const emit = defineEmits<{ open: [index: number] }>()

const { t } = useI18n()

const location = computed(() => {
  if (citation.pageNumber !== null) {
    return t('chat.citations.page', { page: citation.pageNumber })
  }

  return citation.sheetName ? t('chat.citations.sheet', { sheet: citation.sheetName }) : null
})
</script>

<template>
  <button
    type="button"
    class="citation-chip"
    :class="{ 'citation-chip--active': active }"
    :aria-pressed="active"
    @click="emit('open', citation.index)"
  >
    <span class="citation-chip__index">{{ citation.index }}</span>
    <span class="citation-chip__document">{{ citation.documentName }}</span>
    <span v-if="location" class="citation-chip__location">{{ location }}</span>
  </button>
</template>

<style scoped>
.citation-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  max-width: 100%;
  padding: 0.2rem 0.625rem 0.2rem 0.25rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 999px;
  background: var(--p-content-background);
  color: inherit;
  font: inherit;
  font-size: 0.8rem;
  cursor: pointer;
}

.citation-chip:hover,
.citation-chip:focus-visible,
.citation-chip--active {
  border-color: var(--p-primary-400);
  background: var(--p-primary-50);
  outline: none;
}

.citation-chip__index {
  display: grid;
  flex: none;
  place-items: center;
  min-width: 1.3rem;
  height: 1.3rem;
  border-radius: 999px;
  background: var(--p-primary-100);
  color: var(--p-primary-800);
  font-weight: 600;
}

.citation-chip__document {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.citation-chip__location {
  flex: none;
  color: var(--p-text-muted-color);
}
</style>
