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
import type { DocumentFormat, DocumentListItem, DocumentStatus } from '@/api/types/document'
import { useFormat } from '@/composables/useFormat'
import { useNotifications } from '@/composables/useNotifications'
import { useDocumentsStore } from '@/stores/documents'
import DocumentStatusTag from './DocumentStatusTag.vue'

const { knowledgeBaseId, canManage } = defineProps<{
  knowledgeBaseId: string
  canManage: boolean
}>()

const emit = defineEmits<{ deleted: [document: DocumentListItem] }>()

const pageSize = 20
const searchDelayMs = 300

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

const status = shallowRef<DocumentStatus | null>(null)
const search = shallowRef('')
const appliedSearch = shallowRef('')
const page = shallowRef(1)
let searchTimer: ReturnType<typeof setTimeout> | undefined

const statusOptions = computed(() => [
  { value: null, label: t('documents.table.allStatuses') },
  ...(['uploaded', 'extracted', 'chunked', 'indexed', 'failed'] as const).map((value) => ({
    value,
    label: t(`documents.status.${value}`),
  })),
])

const filtered = computed(() => status.value !== null || appliedSearch.value !== '')

async function load(signal: AbortSignal): Promise<void> {
  try {
    const result = await store.load(
      knowledgeBaseId,
      { status: status.value, search: appliedSearch.value, page: page.value, pageSize },
      signal,
    )

    if (!result.success) {
      notify(result.notifications)
    }
  } catch (error) {
    if (!isAbort(error)) {
      throw error
    }
  }
}

function changePage(event: DataTablePageEvent): void {
  page.value = event.page + 1
}

async function download(document: DocumentListItem): Promise<void> {
  const result = await store.download(document.id)

  if (!result.success || !result.data) {
    notify(result.notifications)
    return
  }

  const link = window.document.createElement('a')
  link.href = result.data.url
  link.rel = 'noopener'
  link.click()
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
    appliedSearch.value = value.trim()
  }, searchDelayMs)
})
watch([status, appliedSearch], () => {
  page.value = 1
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
        v-model="status"
        :options="statusOptions"
        option-label="label"
        option-value="value"
        :aria-label="t('documents.table.statusFilter')"
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
        <span class="documents__empty">{{
          t(filtered ? 'documents.table.emptyFiltered' : 'documents.table.empty')
        }}</span>
      </template>

      <Column :header="t('documents.table.name')">
        <template #body="{ data }">
          <span class="documents__name">
            <i :class="formatIcons[(data as DocumentListItem).format]" aria-hidden="true" />
            <span :title="(data as DocumentListItem).fileName">{{
              (data as DocumentListItem).fileName
            }}</span>
          </span>
        </template>
      </Column>
      <Column :header="t('documents.table.size')" class="documents__narrow">
        <template #body="{ data }">{{
          format.bytes((data as DocumentListItem).sizeBytes)
        }}</template>
      </Column>
      <Column :header="t('documents.table.status')" class="documents__narrow">
        <template #body="{ data }">
          <DocumentStatusTag :status="(data as DocumentListItem).status" />
        </template>
      </Column>
      <Column :header="t('documents.table.updatedAt')" class="documents__narrow">
        <template #body="{ data }">{{
          format.dateTime((data as DocumentListItem).updatedAt)
        }}</template>
      </Column>
      <Column :header="t('common.actions')" class="documents__actions">
        <template #body="{ data }">
          <Button
            icon="pi pi-download"
            text
            rounded
            severity="secondary"
            :aria-label="t('documents.actions.download')"
            @click="download(data as DocumentListItem)"
          />
          <Button
            v-if="canManage"
            icon="pi pi-trash"
            text
            rounded
            severity="danger"
            :aria-label="t('documents.actions.delete')"
            @click="confirmDelete(data as DocumentListItem)"
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

.documents__name span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.documents__name i {
  color: var(--p-primary-600);
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
