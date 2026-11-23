<script setup lang="ts">
import Tag from 'primevue/tag'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import type { DocumentStatus } from '@/api/types/document'

const { status } = defineProps<{ status: DocumentStatus }>()

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
</script>

<template>
  <Tag :severity="look.severity" :icon="look.icon" :value="t(`documents.status.${status}`)" />
</template>
