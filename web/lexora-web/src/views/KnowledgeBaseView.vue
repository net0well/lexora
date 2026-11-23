<script setup lang="ts">
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tab from 'primevue/tab'
import TabList from 'primevue/tablist'
import TabPanel from 'primevue/tabpanel'
import TabPanels from 'primevue/tabpanels'
import Tabs from 'primevue/tabs'
import Tag from 'primevue/tag'
import { storeToRefs } from 'pinia'
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { isAbort } from '@/api/http'
import DocumentTable from '@/components/documents/DocumentTable.vue'
import DocumentUploader from '@/components/documents/DocumentUploader.vue'
import KnowledgeBaseSettings from '@/components/knowledgeBases/KnowledgeBaseSettings.vue'
import { useFormat } from '@/composables/useFormat'
import { useNotifications } from '@/composables/useNotifications'
import { useQueryState } from '@/composables/useQueryState'
import { useUpload } from '@/composables/useUpload'
import { useAuthStore } from '@/stores/auth'
import { useDocumentsStore } from '@/stores/documents'
import { useKnowledgeBasesStore } from '@/stores/knowledgeBases'

interface TabEntry {
  value: string
  label: string
  icon: string
}

const { id } = defineProps<{ id: string }>()

const { t } = useI18n()
const query = useQueryState()
const format = useFormat()
const auth = useAuthStore()
const store = useKnowledgeBasesStore()
const { current: base, loadingCurrent } = storeToRefs(store)
const documents = useDocumentsStore()
const { notify } = useNotifications()

const {
  items: uploads,
  add: addUploads,
  cancel: cancelUpload,
  clearFinished: clearUploads,
} = useUpload({
  knowledgeBaseId: () => id,
  onUploaded: (document, knowledgeBaseId) => {
    documents.added(document, knowledgeBaseId)
    store.countDocuments(knowledgeBaseId, 1)
  },
})

const canUpload = computed(() => auth.atLeast('member') && base.value?.status === 'active')

const tabs = computed<TabEntry[]>(() => {
  const entries: TabEntry[] = [
    { value: 'documents', label: t('knowledgeBases.tabs.documents'), icon: 'pi pi-file' },
  ]

  if (auth.atLeast('admin')) {
    entries.push({ value: 'settings', label: t('knowledgeBases.tabs.settings'), icon: 'pi pi-cog' })
  }

  return entries
})

const activeTab = computed(() => {
  const requested = query.text('tab')

  return tabs.value.find((tab) => tab.value === requested)?.value ?? tabs.value[0]?.value ?? ''
})

function selectTab(value: string | number): void {
  query.update({ tab: String(value) })
}

watch(
  () => id,
  async (current, _previous, onCleanup) => {
    const controller = new AbortController()
    onCleanup(() => controller.abort())

    try {
      const result = await store.open(current, controller.signal)

      if (!result.success && result.status !== 404) {
        notify(result.notifications)
      }
    } catch (error) {
      if (!isAbort(error)) {
        throw error
      }
    }
  },
  { immediate: true },
)
</script>

<template>
  <section class="page">
    <RouterLink :to="{ name: 'knowledge-bases' }" class="back-link">
      <i class="pi pi-arrow-left" aria-hidden="true" />
      {{ t('nav.knowledgeBases') }}
    </RouterLink>

    <div v-if="loadingCurrent && !base" class="kb-header">
      <Skeleton width="16rem" height="2rem" />
      <Skeleton width="28rem" height="1rem" />
    </div>

    <div v-else-if="!base" class="empty-state">
      <i class="pi pi-question-circle" aria-hidden="true" />
      <p>{{ t('knowledgeBases.notFound') }}</p>
    </div>

    <template v-else>
      <header class="kb-header">
        <div class="kb-header__title">
          <h1 class="kb-header__name">{{ base.name }}</h1>
          <Tag
            v-if="base.status === 'archived'"
            :value="t('knowledgeBases.archived')"
            severity="secondary"
          />
        </div>
        <p v-if="base.description" class="kb-header__description">{{ base.description }}</p>
        <p class="kb-header__meta">
          {{ t('knowledgeBases.documentCount', { count: base.documentCount }, base.documentCount) }}
          ·
          {{ t('knowledgeBases.createdAt', { date: format.dateTime(base.createdAt) }) }}
        </p>
      </header>

      <Tabs v-if="tabs.length" :value="activeTab" @update:value="selectTab">
        <TabList>
          <Tab v-for="tab in tabs" :key="tab.value" :value="tab.value">
            <i :class="tab.icon" aria-hidden="true" />
            <span>{{ tab.label }}</span>
          </Tab>
        </TabList>
        <TabPanels>
          <TabPanel value="documents">
            <div class="documents-tab">
              <DocumentUploader
                v-if="canUpload"
                :items="uploads"
                @add="addUploads"
                @cancel="cancelUpload"
                @clear="clearUploads"
              />
              <Message v-else-if="base.status === 'archived'" severity="secondary">
                {{ t('documents.upload.archived') }}
              </Message>
              <DocumentTable
                :knowledge-base-id="base.id"
                :can-manage="auth.atLeast('member')"
                @deleted="store.countDocuments(base.id, -1)"
              />
            </div>
          </TabPanel>
          <TabPanel v-if="auth.atLeast('admin')" value="settings">
            <KnowledgeBaseSettings :base="base" />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </template>
  </section>
</template>

<style scoped>
.kb-header {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  margin-bottom: 1.25rem;
}

.kb-header__title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.kb-header__name {
  margin: 0;
}

.kb-header__description {
  max-width: 60rem;
  margin: 0;
}

.kb-header__meta {
  margin: 0;
  color: var(--p-text-muted-color);
}

.documents-tab {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

:deep(.p-tab) {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
</style>
