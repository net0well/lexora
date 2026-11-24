<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable, { type DataTablePageEvent } from 'primevue/datatable'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import { useConfirm } from 'primevue/useconfirm'
import { storeToRefs } from 'pinia'
import { computed, onBeforeUnmount, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { isAbort } from '@/api/http'
import {
  terminalStatuses,
  type DocumentFormat,
  type DocumentListItem,
  type DocumentStatus,
} from '@/api/types/document'
import { useDocumentDownload } from '@/composables/useDocumentDownload'
import { useDocumentStatusPolling } from '@/composables/useDocumentStatusPolling'
import { useFormat } from '@/composables/useFormat'
import { useNotifications } from '@/composables/useNotifications'
import { useQueryState } from '@/composables/useQueryState'
import { useDocumentsStore } from '@/stores/documents'
import DocumentStatusTag from './DocumentStatusTag.vue'

const { knowledgeBaseId, canManage } = defineProps<{
  knowledgeBaseId: string
  canManage: boolean
}>()

const emit = defineEmits<{ deleted: [document: DocumentListItem] }>()

const pageSize = 20
const searchDelayMs = 300

const filterableStatuses: readonly DocumentStatus[] = [
  'uploaded',
  'extracted',
  'chunked',
  'indexed',
  'failed',
]

const formatIcons: Record<DocumentFormat, string> = {
  pdf: 'pi pi-file-pdf',
  docx: 'pi pi-file-word',
  xlsx: 'pi pi-file-excel',
  csv: 'pi pi-file-excel',
  txt: 'pi pi-file',
  markdown: 'pi pi-file',
}

const { t } = useI18n()
const confirm = useConfirm()
const format = useFormat()
const store = useDocumentsStore()
const { items, totalItems, loading } = storeToRefs(store)
const { notify, success } = useNotifications()
const { download } = useDocumentDownload()
const query = useQueryState()

const status = computed<DocumentStatus | null>(() => {
  const value = query.text('status') as DocumentStatus

  return filterableStatuses.includes(value) ? value : null
})
const appliedSearch = computed(() => query.text('search'))
const page = computed(() => query.positiveInteger('page'))
const search = shallowRef(appliedSearch.value)
let searchTimer: ReturnType<typeof setTimeout> | undefined

const statusOptions = computed(() => [
  { value: null, label: t('documents.table.allStatuses') },
  ...filterableStatuses.map((value) => ({
    value,
    label: t(`documents.status.${value}`),
  })),
])

const filtered = computed(() => status.value !== null || appliedSearch.value !== '')

const followedIds = computed(() =>
  items.value.filter((item) => !terminalStatuses.includes(item.status)).map((item) => item.id),
)

useDocumentStatusPolling({
  ids: followedIds,
  fetch: (id, signal) => store.details(id, signal),
  onUpdate: (document) => store.update(document),
})

async function explainFailures(signal: AbortSignal): Promise<void> {
  const unexplained = items.value.filter(
    (item) => item.status === 'failed' && item.failureReason === undefined,
  )
  const results = await Promise.all(unexplained.map((item) => store.details(item.id, signal)))

  for (const result of results) {
    if (result.success && result.data) {
      store.update(result.data)
    }
  }
}

async function load(signal: AbortSignal): Promise<void> {
  try {
    const result = await store.load(
      knowledgeBaseId,
      { status: status.value, search: appliedSearch.value, page: page.value, pageSize },
      signal,
    )

    if (result.success) {
      await explainFailures(signal)
    } else {
      notify(result.notifications)
    }
  } catch (error) {
    if (!isAbort(error)) {
      throw error
    }
  }
}

function changeStatus(value: DocumentStatus | null): void {
  query.update({ status: value, page: null })
}

function changePage(event: DataTablePageEvent): void {
  query.update({ page: event.page === 0 ? null : event.page + 1 })
}

async function reprocess(document: DocumentListItem): Promise<void> {
  const result = await store.reprocess(document.id)

  if (result.success) {
    success(t('documents.actions.reprocessed'))
  } else {
    notify(result.notifications)
  }
}

function confirmReprocess(document: DocumentListItem): void {
  if (document.status === 'failed') {
    void reprocess(document)
    return
  }

  confirm.require({
    header: t('documents.actions.reprocessTitle'),
    message: t('documents.actions.reprocessConfirm', { name: document.fileName }),
    icon: 'pi pi-refresh',
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    acceptProps: { label: t('documents.actions.reprocess') },
    accept: () => reprocess(document),
  })
}

function confirmDelete(document: DocumentListItem): void {
  confirm.require({
    header: t('documents.actions.deleteTitle'),
    message: t('documents.actions.deleteConfirm', { name: document.fileName }),
    icon: 'pi pi-trash',
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    acceptProps: { label: t('documents.actions.delete'), severity: 'danger' },
    accept: async () => {
      const result = await store.remove(document.id)

      if (result.success) {
        success(t('documents.actions.deleted'))
        emit('deleted', document)
      } else {
        notify(result.notifications)
      }
    },
  })
}

watch(search, (value) => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    if (value.trim() !== appliedSearch.value) {
      query.update({ search: value.trim(), page: null })
    }
  }, searchDelayMs)
})
watch(appliedSearch, (value) => {
  if (value !== search.value.trim()) {
    search.value = value
  }
})
watch(
  [() => knowledgeBaseId, status, appliedSearch, page],
  (_values, _previous, onCleanup) => {
    const controller = new AbortController()
    onCleanup(() => controller.abort())
    void load(controller.signal)
  },
  { immediate: true },
)
onBeforeUnmount(() => clearTimeout(searchTimer))
</script>

<template>
  <div class="documents">
    <div class="documents__filters">
      <IconField>
        <InputIcon class="pi pi-search" />
        <InputText
          v-model="search"
          :placeholder="t('documents.table.search')"
          :aria-label="t('documents.table.search')"
        />
      </IconField>
      <Select
        :model-value="status"
        :options="statusOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('documents.table.allStatuses')"
        :aria-label="t('documents.table.statusFilter')"
        @update:model-value="changeStatus"
      />
    </div>

    <DataTable
      :value="items"
      data-key="id"
      lazy
      :loading="loading"
      :paginator="totalItems > pageSize"
      :rows="pageSize"
      :total-records="totalItems"
      :first="(page - 1) * pageSize"
      @page="changePage"
    >
      <template #empty>
        <span v-if="!loading" class="documents__empty">{{
          t(filtered ? 'documents.table.emptyFiltered' : 'documents.table.empty')
        }}</span>
      </template>

      <Column :header="t('documents.table.name')">
        <template #body="{ data: row }">
          <span class="documents__name">
            <i
              :class="['documents__icon', formatIcons[row.format as DocumentFormat]]"
              aria-hidden="true"
            />
            <span class="documents__file-name" :title="row.fileName">{{ row.fileName }}</span>
          </span>
          <small v-if="row.status === 'failed' && row.failureReason" class="documents__failure">
            {{ t('documents.failure', { reason: row.failureReason }) }}
          </small>
        </template>
      </Column>
      <Column :header="t('documents.table.size')" class="documents__narrow">
        <template #body="{ data: row }">{{ format.bytes(row.sizeBytes) }}</template>
      </Column>
      <Column :header="t('documents.table.status')" class="documents__narrow">
        <template #body="{ data: row }">
          <DocumentStatusTag
            :status="row.status"
            :chunk-count="row.chunkCount"
            :embedded-chunk-count="row.embeddedChunkCount"
          />
        </template>
      </Column>
      <Column :header="t('documents.table.updatedAt')" class="documents__narrow">
        <template #body="{ data: row }">{{ format.dateTime(row.updatedAt) }}</template>
      </Column>
      <Column :header="t('common.actions')" class="documents__actions">
        <template #body="{ data: row }">
          <Button
            icon="pi pi-download"
            text
            rounded
            severity="secondary"
            :aria-label="t('documents.actions.download')"
            @click="download(row.id)"
          />
          <Button
            v-if="canManage && (row.status === 'failed' || row.status === 'indexed')"
            icon="pi pi-refresh"
            text
            rounded
            severity="secondary"
            :aria-label="t('documents.actions.reprocess')"
            @click="confirmReprocess(row)"
          />
          <Button
            v-if="canManage"
            icon="pi pi-trash"
            text
            rounded
            severity="danger"
            :aria-label="t('documents.actions.delete')"
            @click="confirmDelete(row)"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<style scoped>
.documents {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.documents__filters {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.documents__name {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  max-width: 32rem;
}

.documents__file-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.documents__icon {
  color: var(--p-primary-600);
}

.documents__failure {
  display: block;
  margin-top: 0.25rem;
  color: var(--p-red-600);
}

.documents__empty {
  color: var(--p-text-muted-color);
}

:deep(.documents__narrow) {
  white-space: nowrap;
}

:deep(.documents__actions) {
  width: 1%;
  white-space: nowrap;
}
</style>
