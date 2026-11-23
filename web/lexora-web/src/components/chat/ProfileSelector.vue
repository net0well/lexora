<script setup lang="ts">
import Select from 'primevue/select'
import { storeToRefs } from 'pinia'
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'

import { useAiProfilesStore } from '@/stores/aiProfiles'

const profile = defineModel<string | null>({ default: null })

const { t } = useI18n()
const store = useAiProfilesStore()
const { selectable, defaultProfile } = storeToRefs(store)

const options = computed(() =>
  selectable.value.map((choice) => ({
    value: choice.name,
    label: choice.available
      ? `${choice.name} · ${choice.model}`
      : t('chat.profile.unavailable', { name: choice.name }),
    disabled: !choice.available,
  })),
)

onMounted(() => void store.load())
</script>

<template>
  <Select
    v-if="options.length > 1"
    v-model="profile"
    :options="options"
    option-label="label"
    option-value="value"
    option-disabled="disabled"
    :placeholder="t('chat.profile.default', { name: defaultProfile ?? '' })"
    :aria-label="t('chat.profile.label')"
    show-clear
    size="small"
  />
</template>
