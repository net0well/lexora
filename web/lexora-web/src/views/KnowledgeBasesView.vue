<script setup lang="ts">
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Paginator, { type PageState } from 'primevue/paginator'
import SelectButton from 'primevue/selectbutton'
import Skeleton from 'primevue/skeleton'
import { storeToRefs } from 'pinia'
import { computed, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import { isAbort } from '@/api/http'
import type { KnowledgeBaseFields, KnowledgeBaseStatus } from '@/api/types/knowledgeBase'
import KnowledgeBaseCard from '@/components/knowledgeBases/KnowledgeBaseCard.vue'
import KnowledgeBaseForm from '@/components/knowledgeBases/KnowledgeBaseForm.vue'
import { useNotifications } from '@/composables/useNotifications'
import { useQueryState } from '@/composables/useQueryState'
import { useAuthStore } from '@/stores/auth'
import { useKnowledgeBasesStore } from '@/stores/knowledgeBases'

const pageSize = 24

const { t } = useI18n()
const router = useRouter()
const query = useQueryState()
const auth = useAuthStore()
const store = useKnowledgeBasesStore()
const { items, totalItems, loadingList } = storeToRefs(store)
const { notify, success } = useNotifications()

const creating = shallowRef(false)
const saving = shallowRef(false)

const status = computed<KnowledgeBaseStatus>(() =>
  query.text('status') === 'archived' ? 'archived' : 'active',
)
const page = computed(() => query.positiveInteger('page'))

const statusOptions = computed(() => [
  { value: 'active', label: t('knowledgeBases.filter.active') },
  { value: 'archived', label: t('knowledgeBases.filter.archived') },
])

function changeStatus(value: KnowledgeBaseStatus): void {
  query.update({ status: value === 'active' ? null : value, page: null })
}

function changePage(state: PageState): void {
  query.update({ page: state.page === 0 ? null : state.page + 1 })
}

async function create(fields: KnowledgeBaseFields): Promise<void> {
  saving.value = true

  try {
    const result = await store.create(fields)

    if (!result.success || !result.data) {
      notify(result.notifications)
      return
    }

    creating.value = false
    success(t('knowledgeBases.created', { name: result.data.name }))
    await router.push({ name: 'knowledge-base', params: { id: result.data.id } })
  } finally {
    saving.value = false
  }
}

watch(
  [status, page],
  async ([currentStatus, currentPage], _previous, onCleanup) => {
    const controller = new AbortController()
    onCleanup(() => controller.abort())

    try {
      const result = await store.load(
        { status: currentStatus, page: currentPage, pageSize },
        controller.signal,
      )

      if (!result.success) {
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
    <header class="page__header">
      <div>
        <h1>{{ t('nav.knowledgeBases') }}</h1>
        <p class="page__subtitle">{{ t('knowledgeBases.subtitle') }}</p>
      </div>
      <div class="page__actions">
        <SelectButton
          :model-value="status"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          :aria-label="t('knowledgeBases.filter.label')"
          @update:model-value="changeStatus"
        />
        <Button
          v-if="auth.atLeast('admin')"
          :label="t('knowledgeBases.new')"
          icon="pi pi-plus"
          @click="creating = true"
        />
      </div>
    </header>

    <div v-if="loadingList && items.length === 0" class="kb-grid">
      <Skeleton v-for="index in 6" :key="index" height="9.5rem" border-radius="12px" />
    </div>

    <div v-else-if="items.length === 0" class="empty-state">
      <i class="pi pi-book" aria-hidden="true" />
      <p>{{ t(status === 'active' ? 'knowledgeBases.empty' : 'knowledgeBases.emptyArchived') }}</p>
      <p v-if="status === 'active' && auth.atLeast('admin')" class="empty-state__hint">
        {{ t('knowledgeBases.emptyAdminHint') }}
      </p>
    </div>

    <div v-else class="kb-grid">
      <KnowledgeBaseCard v-for="item in items" :key="item.id" :item="item" />
    </div>

    <Paginator
      v-if="totalItems > pageSize"
      :rows="pageSize"
      :total-records="totalItems"
      :first="(page - 1) * pageSize"
      @page="changePage"
    />

    <Dialog
      v-model:visible="creating"
      modal
      :header="t('knowledgeBases.new')"
      :style="{ width: 'min(36rem, 95vw)' }"
    >
      <KnowledgeBaseForm
        v-if="creating"
        id-prefix="new-knowledge-base"
        :submit-label="t('common.create')"
        :submitting="saving"
        cancelable
        @submit="create"
        @cancel="creating = false"
      />
    </Dialog>
  </section>
</template>

<style scoped>
.kb-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr));
  gap: 1rem;
}
</style>
