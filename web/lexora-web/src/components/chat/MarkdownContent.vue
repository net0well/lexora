<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { linkCitations } from '@/utils/citationMarkers'
import { renderMarkdown } from '@/utils/markdown'

const { source, citations = [] } = defineProps<{
  source: string
  citations?: readonly number[]
}>()

const emit = defineEmits<{ cite: [index: number] }>()

const { t } = useI18n()

const html = computed(() =>
  linkCitations(renderMarkdown(source), new Set(citations), (index) =>
    t('chat.citations.open', { index }),
  ),
)

function clicked(event: MouseEvent): void {
  const marker = (event.target as HTMLElement | null)?.closest<HTMLElement>('[data-citation]')

  if (marker?.dataset.citation) {
    emit('cite', Number(marker.dataset.citation))
  }
}
</script>

<template>
  <!-- eslint-disable-next-line vue/no-v-html -- renderMarkdown sanitizes; linkCitations only adds numbered buttons -->
  <div class="markdown" @click="clicked" v-html="html" />
</template>

<style scoped>
.markdown {
  overflow-wrap: anywhere;
}

.markdown :deep(p) {
  margin: 0 0 0.6rem;
}

.markdown :deep(p:last-child) {
  margin-bottom: 0;
}

.markdown :deep(ul),
.markdown :deep(ol) {
  margin: 0 0 0.6rem;
  padding-left: 1.25rem;
}

.markdown :deep(code) {
  padding: 0.1rem 0.3rem;
  border-radius: 4px;
  background: var(--p-surface-100);
  font-size: 0.9em;
}

.markdown :deep(pre) {
  overflow-x: auto;
  padding: 0.75rem;
  border-radius: 8px;
  background: var(--p-surface-100);
}

.markdown :deep(a) {
  color: var(--p-primary-700);
}

.markdown :deep(.citation-marker) {
  display: inline-grid;
  place-items: center;
  min-width: 1.3rem;
  height: 1.3rem;
  margin: 0 0.125rem;
  padding: 0 0.3rem;
  border: none;
  border-radius: 999px;
  background: var(--p-primary-100);
  color: var(--p-primary-800);
  font: inherit;
  font-size: 0.75rem;
  font-weight: 600;
  vertical-align: 0.15em;
  cursor: pointer;
}

.markdown :deep(.citation-marker:hover),
.markdown :deep(.citation-marker:focus-visible) {
  background: var(--p-primary-600);
  color: var(--p-primary-contrast-color);
  outline: none;
}
</style>
