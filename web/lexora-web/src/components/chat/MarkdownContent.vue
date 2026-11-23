<script setup lang="ts">
import { computed } from 'vue'

import { renderMarkdown } from '@/utils/markdown'

const { source } = defineProps<{ source: string }>()

const html = computed(() => renderMarkdown(source))
</script>

<template>
  <!-- eslint-disable-next-line vue/no-v-html -- the HTML comes from renderMarkdown, which sanitizes it -->
  <div class="markdown" v-html="html" />
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
</style>
