<script setup lang="ts">
import Button from 'primevue/button'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import SelectButton from 'primevue/selectbutton'
import Skeleton from 'primevue/skeleton'
import { computed, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { searchRules, type SearchMode, type SearchRequest } from '@/api/types/search'
import { useDocumentDownload } from '@/composables/useDocumentDownload'
import { useKnowledgeBaseSearch } from '@/composables/useKnowledgeBaseSearch'
import { useNotifications } from '@/composables/useNotifications'
import { useQueryState } from '@/composables/useQueryState'
import { useKnowledgeBasesStore } from '@/stores/knowledgeBases'
import SearchHitCard from './SearchHitCard.vue'

const { knowledgeBaseId } = defineProps<{ knowledgeBaseId: string }>()

const { t } = useI18n()
const query = useQueryState()
const store = useKnowledgeBasesStore()
const { notify } = useNotifications()
const { download } = useDocumentDownload()

const executed = computed<SearchRequest | null>(() => {
  const text = query.text('q').trim()
  const mode = query.text('mode') as SearchMode

  return text ? { query: text, mode: searchRules.modes.includes(mode) ? mode : 'hybrid' } : null
})

const draft = shallowRef(executed.value?.query ?? '')
const mode = shallowRef<SearchMode>(executed.value?.mode ?? 'hybrid')
const submitted = shallowRef(false)

const { hits, searched, pending, refresh } = useKnowledgeBaseSearch({
  knowledgeBaseId: () => knowledgeBaseId,
  request: executed,
  search: (id, request, signal) => store.search(id, request, signal),
  onFailure: notify,
})

const modeOptions = computed(() =>
  searchRules.modes.map((value) => ({ value, label: t(`search.modes.${value}`) })),
)

const error = computed(() => {
  if (!submitted.value) {
    return null
  }

  const text = draft.value.trim()

  if (!text) {
    return t('search.queryRequired')
  }

  return text.length > searchRules.queryMaxLength
    ? t('search.queryTooLong', { max: searchRules.queryMaxLength })
    : null
})

function searchFor(text: string, chosenMode: SearchMode): void {
  if (executed.value?.query === text && executed.value.mode === chosenMode) {
    refresh()
    return
  }

  query.update({ q: text, mode: chosenMode === 'hybrid' ? null : chosenMode })
}

function submit(): void {
  submitted.value = true

  if (!error.value) {
    searchFor(draft.value.trim(), mode.value)
  }
}

function changeMode(value: SearchMode): void {
  mode.value = value

  if (executed.value) {
    searchFor(executed.value.query, value)
  }
}

watch(executed, (value) => {
  if (value) {
    draft.value = value.query
    mode.value = value.mode
  }
})
</script>

<template>
  <div class="search">
    <form class="search__form" role="search" novalidate @submit.prevent="submit">
      <div class="search__bar">
        <IconField class="search__field">
          <InputIcon class="pi pi-search" />
          <InputText
            v-model="draft"
            :placeholder="t('search.placeholder')"
            :aria-label="t('search.placeholder')"
            :maxlength="searchRules.queryMaxLength"
            :invalid="Boolean(error)"
            fluid
          />
        </IconField>
        <Button type="submit" :label="t('search.submit')" icon="pi pi-search" :loading="pending" />
      </div>
      <small v-if="error" class="field__error">{{ error }}</small>
      <div class="search__modes">
        <SelectButton
          :model-value="mode"
          :options="modeOptions"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          :aria-label="t('search.modeLabel')"
          @update:model-value="changeMode"
        />
        <small class="search__hint">{{ t(`search.modeHints.${mode}`) }}</small>
      </div>
    </form>

    <div v-if="pending" class="search__results">
      <Skeleton v-for="index in 3" :key="index" height="7rem" border-radius="12px" />
    </div>

    <template v-else-if="searched">
      <p class="search__summary" aria-live="polite">
        {{ t('search.results', { count: hits.length }, hits.length) }}
      </p>
      <ol v-if="hits.length" class="search__results">
        <li v-for="(hit, index) in hits" :key="hit.chunkId">
          <SearchHitCard :hit="hit" :rank="index + 1" @download="download" />
        </li>
      </ol>
      <div v-else class="empty-state">
        <i class="pi pi-search" aria-hidden="true" />
        <p>{{ t('search.empty') }}</p>
      </div>
    </template>

    <div v-else class="empty-state">
      <i class="pi pi-search" aria-hidden="true" />
      <p>{{ t('search.idle') }}</p>
    </div>
  </div>
</template>

<style scoped>
.search {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  max-width: 60rem;
}

.search__form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.search__bar {
  display: flex;
  gap: 0.75rem;
}

.search__field {
  flex: 1;
}

.search__modes {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
}

.search__hint,
.search__summary {
  color: var(--p-text-muted-color);
}

.search__summary {
  margin: 0;
}

.search__results {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin: 0;
  padding: 0;
  list-style: none;
}
</style>
