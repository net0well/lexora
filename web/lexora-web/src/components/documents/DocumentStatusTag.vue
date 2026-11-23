<script setup lang="ts">
import Tag from 'primevue/tag'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import type { DocumentStatus } from '@/api/types/document'

const {
  status,
  chunkCount = 0,
  embeddedChunkCount = 0,
} = defineProps<{
  status: DocumentStatus
  chunkCount?: number
  embeddedChunkCount?: number
}>()

const { t } = useI18n()

const appearance: Record<DocumentStatus, { severity: string; icon: string }> = {
  uploaded: { severity: 'secondary', icon: 'pi pi-clock' },
  extracted: { severity: 'info', icon: 'pi pi-spin pi-spinner' },
  chunked: { severity: 'info', icon: 'pi pi-spin pi-spinner' },
  indexed: { severity: 'success', icon: 'pi pi-check' },
  failed: { severity: 'danger', icon: 'pi pi-times' },
  deleted: { severity: 'secondary', icon: 'pi pi-trash' },
}

const look = computed(() => appearance[status])

const label = computed(() =>
  status === 'chunked' && chunkCount > 0
    ? t('documents.status.chunkedProgress', { done: embeddedChunkCount, total: chunkCount })
    : t(`documents.status.${status}`),
)
</script>

<template>
  <Tag :severity="look.severity" :icon="look.icon" :value="label" />
</template>
