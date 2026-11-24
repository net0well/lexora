<script setup lang="ts">
import Button from 'primevue/button'
import Drawer from 'primevue/drawer'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import type { Citation } from '@/api/types/conversation'
import { useDocumentDownload } from '@/composables/useDocumentDownload'
import CitationChip from './CitationChip.vue'

const index = defineModel<number | null>('index', { required: true })

const { citations } = defineProps<{ citations: readonly Citation[] }>()

const { t } = useI18n()
const { download } = useDocumentDownload()

const citation = computed(() => citations.find((item) => item.index === index.value) ?? null)

const visible = computed({
  get: () => citation.value !== null,
  set: (open) => {
    if (!open) {
      index.value = null
    }
  },
})

const location = computed(() => {
  if (!citation.value) {
    return null
  }

  if (citation.value.pageNumber !== null) {
    return t('chat.citations.page', { page: citation.value.pageNumber })
  }

  return citation.value.sheetName
    ? t('chat.citations.sheet', { sheet: citation.value.sheetName })
    : null
})
</script>

<template>
  <Drawer
    v-model:visible="visible"
    position="right"
    :header="citation ? t('chat.citations.title', { index: citation.index }) : ''"
    :style="{ width: 'min(28rem, 100vw)' }"
  >
    <div v-if="citation" class="citation">
      <div v-if="citations.length > 1" class="citation__others">
        <CitationChip
          v-for="item in citations"
          :key="item.index"
          :citation="item"
          :active="item.index === citation.index"
          @open="index = $event"
        />
      </div>

      <div class="citation__source">
        <i class="pi pi-file citation__icon" aria-hidden="true" />
        <div>
          <strong class="citation__document">{{ citation.documentName }}</strong>
          <p v-if="location" class="citation__location">{{ location }}</p>
        </div>
      </div>

      <section>
        <h3 class="citation__heading">{{ t('chat.citations.excerpt') }}</h3>
        <blockquote class="citation__excerpt">{{ citation.excerpt }}</blockquote>
      </section>

      <Button
        :label="t('chat.citations.download')"
        icon="pi pi-download"
        outlined
        @click="download(citation.documentId)"
      />
    </div>
  </Drawer>
</template>

<style scoped>
.citation {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.citation__others {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
}

.citation__source {
  display: flex;
  align-items: flex-start;
  gap: 0.625rem;
}

.citation__icon {
  margin-top: 0.2rem;
  color: var(--p-primary-600);
  font-size: 1.25rem;
}

.citation__document {
  overflow-wrap: anywhere;
}

.citation__location {
  margin: 0.125rem 0 0;
  color: var(--p-text-muted-color);
}

.citation__heading {
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--p-text-muted-color);
}

.citation__excerpt {
  margin: 0;
  padding: 0.75rem 1rem;
  border-left: 3px solid var(--p-primary-400);
  border-radius: 0 8px 8px 0;
  background: var(--p-surface-50);
  white-space: pre-line;
}
</style>
