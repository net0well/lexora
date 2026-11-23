<script setup lang="ts">
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import { computed, reactive, shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'

import { knowledgeBaseRules, type KnowledgeBaseFields } from '@/api/types/knowledgeBase'

const {
  initial = null,
  submitting = false,
  submitLabel,
  cancelable = false,
  idPrefix = 'knowledge-base',
} = defineProps<{
  initial?: KnowledgeBaseFields | null
  submitting?: boolean
  submitLabel: string
  cancelable?: boolean
  idPrefix?: string
}>()

const emit = defineEmits<{
  submit: [fields: KnowledgeBaseFields]
  cancel: []
}>()

const { t } = useI18n()
const rules = knowledgeBaseRules

// The form edits a copy taken once; the parent remounts it (by key) to start from other values.
const form = reactive({
  name: initial?.name ?? '',
  description: initial?.description ?? '',
  chunkMaxTokens: initial?.chunkMaxTokens ?? rules.defaultChunkMaxTokens,
  chunkOverlapTokens: initial?.chunkOverlapTokens ?? rules.defaultChunkOverlapTokens,
})
const touched = shallowRef(false)

const errors = computed(() => {
  if (!touched.value) {
    return {}
  }

  const name = form.name.trim()
  const maxTokens = form.chunkMaxTokens ?? 0
  const overlap = form.chunkOverlapTokens ?? -1

  return {
    name:
      name.length < rules.nameMinLength || name.length > rules.nameMaxLength
        ? t('knowledgeBases.form.nameLength', {
            min: rules.nameMinLength,
            max: rules.nameMaxLength,
          })
        : null,
    description:
      form.description.length > rules.descriptionMaxLength
        ? t('knowledgeBases.form.descriptionLength', { max: rules.descriptionMaxLength })
        : null,
    chunkMaxTokens:
      maxTokens < rules.chunkMaxTokensMin || maxTokens > rules.chunkMaxTokensMax
        ? t('knowledgeBases.form.chunkMaxTokensRange', {
            min: rules.chunkMaxTokensMin,
            max: rules.chunkMaxTokensMax,
          })
        : null,
    chunkOverlapTokens:
      overlap < 0 || overlap > Math.floor(maxTokens / 2)
        ? t('knowledgeBases.form.chunkOverlapRange', { max: Math.floor(maxTokens / 2) })
        : null,
  }
})

const hasErrors = computed(() => Object.values(errors.value).some(Boolean))

function submit(): void {
  touched.value = true

  if (hasErrors.value) {
    return
  }

  emit('submit', {
    name: form.name.trim(),
    description: form.description.trim() || null,
    chunkMaxTokens: form.chunkMaxTokens,
    chunkOverlapTokens: form.chunkOverlapTokens,
  })
}
</script>

<template>
  <form class="kb-form" novalidate @submit.prevent="submit">
    <div class="field">
      <label :for="`${idPrefix}-name`">{{ t('knowledgeBases.form.name') }}</label>
      <InputText
        :id="`${idPrefix}-name`"
        v-model="form.name"
        :maxlength="rules.nameMaxLength"
        :invalid="Boolean(errors.name)"
        fluid
      />
      <small v-if="errors.name" class="field__error">{{ errors.name }}</small>
    </div>

    <div class="field">
      <label :for="`${idPrefix}-description`">{{ t('knowledgeBases.form.description') }}</label>
      <Textarea
        :id="`${idPrefix}-description`"
        v-model="form.description"
        rows="3"
        auto-resize
        :maxlength="rules.descriptionMaxLength"
        :invalid="Boolean(errors.description)"
        fluid
      />
      <small v-if="errors.description" class="field__error">{{ errors.description }}</small>
    </div>

    <fieldset class="kb-form__chunking">
      <legend>{{ t('knowledgeBases.form.chunking') }}</legend>
      <p class="kb-form__hint">{{ t('knowledgeBases.form.chunkingHint') }}</p>
      <div class="kb-form__row">
        <div class="field">
          <label :for="`${idPrefix}-max-tokens`">{{
            t('knowledgeBases.form.chunkMaxTokens')
          }}</label>
          <InputNumber
            v-model="form.chunkMaxTokens"
            :input-id="`${idPrefix}-max-tokens`"
            :min="0"
            :use-grouping="false"
            :invalid="Boolean(errors.chunkMaxTokens)"
            fluid
          />
          <small v-if="errors.chunkMaxTokens" class="field__error">{{
            errors.chunkMaxTokens
          }}</small>
        </div>
        <div class="field">
          <label :for="`${idPrefix}-overlap-tokens`">{{
            t('knowledgeBases.form.chunkOverlapTokens')
          }}</label>
          <InputNumber
            v-model="form.chunkOverlapTokens"
            :input-id="`${idPrefix}-overlap-tokens`"
            :min="0"
            :use-grouping="false"
            :invalid="Boolean(errors.chunkOverlapTokens)"
            fluid
          />
          <small v-if="errors.chunkOverlapTokens" class="field__error">{{
            errors.chunkOverlapTokens
          }}</small>
        </div>
      </div>
    </fieldset>

    <div class="kb-form__actions">
      <Button
        v-if="cancelable"
        type="button"
        :label="t('common.cancel')"
        severity="secondary"
        text
        @click="emit('cancel')"
      />
      <Button type="submit" :label="submitLabel" :loading="submitting" />
    </div>
  </form>
</template>

<style scoped>
.kb-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.kb-form__chunking {
  margin: 0;
  padding: 0.75rem 1rem 1rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 8px;
}

.kb-form__chunking legend {
  padding: 0 0.25rem;
  font-weight: 500;
}

.kb-form__hint {
  margin: 0 0 0.75rem;
  color: var(--p-text-muted-color);
  font-size: 0.875rem;
}

.kb-form__row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
  gap: 1rem;
}

.kb-form__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
