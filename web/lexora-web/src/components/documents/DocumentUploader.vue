<script setup lang="ts">
import Button from 'primevue/button'
import ProgressBar from 'primevue/progressbar'
import { computed, shallowRef, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'

import { documentRules } from '@/api/types/document'
import { useFormat } from '@/composables/useFormat'
import { useNotifications } from '@/composables/useNotifications'
import type { UploadItem } from '@/stores/uploads'

const { items } = defineProps<{ items: readonly UploadItem[] }>()

const emit = defineEmits<{
  add: [files: File[]]
  cancel: [key: number]
  clear: []
}>()

const { t } = useI18n()
const format = useFormat()
const { describe } = useNotifications()
const input = useTemplateRef<HTMLInputElement>('input')
const dragging = shallowRef(false)

const accept = documentRules.extensions.join(',')
const hasFinished = computed(() =>
  items.some((item) => item.state !== 'queued' && item.state !== 'uploading'),
)

function choose(): void {
  input.value?.click()
}

function picked(event: Event): void {
  const target = event.target as HTMLInputElement
  emit('add', [...(target.files ?? [])])
  target.value = ''
}

function dropped(event: DragEvent): void {
  dragging.value = false
  emit('add', [...(event.dataTransfer?.files ?? [])])
}

function stateText(item: UploadItem): string {
  if (item.state === 'uploading') {
    return t('documents.upload.uploading', { progress: item.progress })
  }

  if (item.rejection === 'size') {
    return t('documents.upload.tooLarge', { max: format.bytes(documentRules.maxFileSizeBytes) })
  }

  if (item.rejection) {
    return t(`documents.upload.${item.rejection}`)
  }

  if (item.state === 'failed') {
    return item.notifications.map(describe).join(' ') || t('notifications.unexpected')
  }

  return t(`documents.upload.${item.state}`)
}
</script>

<template>
  <div class="uploader">
    <div
      class="uploader__zone"
      :class="{ 'uploader__zone--active': dragging }"
      role="button"
      tabindex="0"
      :aria-label="t('documents.upload.drop')"
      @click="choose"
      @keydown.enter.prevent="choose"
      @keydown.space.prevent="choose"
      @dragenter.prevent="dragging = true"
      @dragover.prevent="dragging = true"
      @dragleave.prevent="dragging = false"
      @drop.prevent="dropped"
    >
      <i class="pi pi-cloud-upload uploader__icon" aria-hidden="true" />
      <strong class="uploader__title">{{ t('documents.upload.drop') }}</strong>
      <span>{{
        t('documents.upload.hint', { max: format.bytes(documentRules.maxFileSizeBytes) })
      }}</span>
      <input
        ref="input"
        type="file"
        class="uploader__input"
        multiple
        :accept="accept"
        tabindex="-1"
        aria-hidden="true"
        @change="picked"
      />
    </div>

    <ul v-if="items.length" class="uploader__list" aria-live="polite">
      <li
        v-for="item in items"
        :key="item.key"
        class="uploader__item"
        :class="`uploader__item--${item.state}`"
      >
        <div class="uploader__file">
          <span class="uploader__name" :title="item.name">{{ item.name }}</span>
          <span class="uploader__size">{{ format.bytes(item.size) }}</span>
        </div>
        <div class="uploader__status">
          <ProgressBar
            v-if="item.state === 'uploading' || item.state === 'queued'"
            :value="item.progress"
            :show-value="false"
            class="uploader__progress"
          />
          <span class="uploader__state">{{ stateText(item) }}</span>
        </div>
        <Button
          v-if="item.state === 'uploading' || item.state === 'queued'"
          icon="pi pi-times"
          text
          rounded
          severity="secondary"
          size="small"
          :aria-label="t('documents.upload.cancel')"
          @click="emit('cancel', item.key)"
        />
      </li>
    </ul>

    <div v-if="hasFinished" class="uploader__footer">
      <Button
        :label="t('documents.upload.clearFinished')"
        text
        size="small"
        severity="secondary"
        @click="emit('clear')"
      />
    </div>
  </div>
</template>

<style scoped>
.uploader {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.uploader__zone {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
  padding: 1.5rem;
  border: 2px dashed var(--p-content-border-color);
  border-radius: 12px;
  background: var(--p-content-background);
  color: var(--p-text-muted-color);
  text-align: center;
  cursor: pointer;
  transition:
    border-color 0.15s,
    background 0.15s;
}

.uploader__zone:hover,
.uploader__zone:focus-visible,
.uploader__zone--active {
  border-color: var(--p-primary-400);
  background: var(--p-primary-50);
  outline: none;
}

.uploader__icon {
  font-size: 1.75rem;
  color: var(--p-primary-600);
}

.uploader__title {
  color: var(--p-text-color);
}

.uploader__input {
  display: none;
}

.uploader__list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.uploader__item {
  display: grid;
  grid-template-columns: minmax(10rem, 1fr) minmax(10rem, 22rem) auto;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--p-content-border-color);
  border-radius: 8px;
  background: var(--p-content-background);
}

.uploader__file {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.uploader__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.uploader__size,
.uploader__state {
  color: var(--p-text-muted-color);
  font-size: 0.85rem;
}

.uploader__progress {
  height: 0.4rem;
}

.uploader__item--done .uploader__state {
  color: var(--p-green-700);
}

.uploader__status {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.uploader__item--failed .uploader__state,
.uploader__item--rejected .uploader__state {
  color: var(--p-red-600);
}

.uploader__footer {
  display: flex;
  justify-content: flex-end;
}
</style>
