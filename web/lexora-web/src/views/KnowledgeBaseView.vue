<script setup lang="ts">
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
import { useRoute, useRouter } from 'vue-router'

import KnowledgeBaseSettings from '@/components/knowledgeBases/KnowledgeBaseSettings.vue'
import { useFormat } from '@/composables/useFormat'
import { useNotifications } from '@/composables/useNotifications'
import { useAuthStore } from '@/stores/auth'
import { useKnowledgeBasesStore } from '@/stores/knowledgeBases'

interface TabEntry {
  value: string
  label: string
  icon: string
}

const props = defineProps<{ id: string }>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const format = useFormat()
const auth = useAuthStore()
const store = useKnowledgeBasesStore()
const { current: base, loadingCurrent } = storeToRefs(store)
const { notify } = useNotifications()

const tabs = computed<TabEntry[]>(() => {
  const entries: TabEntry[] = []

  if (auth.atLeast('admin')) {
    entries.push({ value: 'settings', label: t('knowledgeBases.tabs.settings'), icon: 'pi pi-cog' })
  }

  return entries
})

const activeTab = computed(() => {
  const requested = route.query.tab

  return tabs.value.find((tab) => tab.value === requested)?.value ?? tabs.value[0]?.value ?? ''
})

function selectTab(value: string | number): void {
  void router.replace({ query: { ...route.query, tab: String(value) } })
}

watch(
  () => props.id,
  async (id) => {
    const result = await store.open(id)

    if (!result.success && result.status !== 404) {
      notify(result.notifications)
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
          <h1>{{ base.name }}</h1>
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

.kb-header__title h1 {
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

:deep(.p-tab) {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
</style>
