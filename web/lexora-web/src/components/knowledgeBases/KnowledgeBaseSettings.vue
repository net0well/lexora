<script setup lang="ts">
import Button from 'primevue/button'
import { useConfirm } from 'primevue/useconfirm'
import { shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'

import type { KnowledgeBase, KnowledgeBaseFields } from '@/api/types/knowledgeBase'
import { useNotifications } from '@/composables/useNotifications'
import { useKnowledgeBasesStore } from '@/stores/knowledgeBases'
import KnowledgeBaseForm from './KnowledgeBaseForm.vue'

const { base } = defineProps<{ base: KnowledgeBase }>()

const { t } = useI18n()
const confirm = useConfirm()
const store = useKnowledgeBasesStore()
const { notify, success } = useNotifications()
const saving = shallowRef(false)

async function save(fields: KnowledgeBaseFields): Promise<void> {
  saving.value = true

  try {
    const result = await store.update(base.id, fields)

    if (result.success) {
      success(t('knowledgeBases.settings.saved'))
    } else {
      notify(result.notifications)
    }
  } finally {
    saving.value = false
  }
}

function confirmReindex(): void {
  confirm.require({
    header: t('knowledgeBases.settings.reindexTitle'),
    message: t('knowledgeBases.settings.reindexConfirm'),
    icon: 'pi pi-refresh',
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    acceptProps: { label: t('knowledgeBases.settings.reindex') },
    accept: async () => {
      const result = await store.reindex(base.id)

      if (result.success && result.data) {
        success(
          t(
            'knowledgeBases.settings.reindexed',
            { count: result.data.documentCount },
            result.data.documentCount,
          ),
        )
      } else {
        notify(result.notifications)
      }
    },
  })
}

function confirmArchive(): void {
  confirm.require({
    header: t('knowledgeBases.settings.archiveTitle'),
    message: t('knowledgeBases.settings.archiveConfirm', { name: base.name }),
    icon: 'pi pi-exclamation-triangle',
    rejectProps: { label: t('common.cancel'), severity: 'secondary', text: true },
    acceptProps: { label: t('knowledgeBases.settings.archive'), severity: 'danger' },
    accept: async () => {
      const result = await store.archive(base.id)

      if (result.success) {
        success(t('knowledgeBases.settings.archived'))
      } else {
        notify(result.notifications)
      }
    },
  })
}
</script>

<template>
  <div class="settings">
    <section class="settings__section">
      <h2 class="settings__heading">{{ t('knowledgeBases.settings.general') }}</h2>
      <KnowledgeBaseForm
        :key="base.id"
        id-prefix="edit-knowledge-base"
        :initial="base"
        :submit-label="t('common.save')"
        :submitting="saving"
        @submit="save"
      />
    </section>

    <section class="settings__section">
      <h2 class="settings__heading">{{ t('knowledgeBases.settings.maintenance') }}</h2>
      <div class="settings__action">
        <div>
          <strong>{{ t('knowledgeBases.settings.reindex') }}</strong>
          <p class="settings__hint">{{ t('knowledgeBases.settings.reindexHint') }}</p>
        </div>
        <Button
          :label="t('knowledgeBases.settings.reindex')"
          icon="pi pi-refresh"
          severity="secondary"
          outlined
          @click="confirmReindex"
        />
      </div>
      <div v-if="base.status === 'active'" class="settings__action">
        <div>
          <strong>{{ t('knowledgeBases.settings.archive') }}</strong>
          <p class="settings__hint">{{ t('knowledgeBases.settings.archiveHint') }}</p>
        </div>
        <Button
          :label="t('knowledgeBases.settings.archive')"
          icon="pi pi-box"
          severity="danger"
          outlined
          @click="confirmArchive"
        />
      </div>
    </section>
  </div>
</template>

<style scoped>
.settings {
  display: grid;
  gap: 1.5rem;
  max-width: 44rem;
}

.settings__section {
  padding: 1.25rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 12px;
  background: var(--p-content-background);
}

.settings__heading {
  font-size: 1rem;
}

.settings__action {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.75rem 0;
}

.settings__action + .settings__action {
  border-top: 1px solid var(--p-content-border-color);
}

.settings__hint {
  margin: 0.25rem 0 0;
  color: var(--p-text-muted-color);
}
</style>
