<script setup lang="ts">
import Select from 'primevue/select'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { rememberLocale, supportedLocales, type AppLocale } from '@/i18n'

const { locale, t } = useI18n()

const options = computed(() =>
  supportedLocales.map((value) => ({ value, label: t(`locale.${value}`) })),
)

function change(value: AppLocale): void {
  locale.value = value
  rememberLocale(value)
}
</script>

<template>
  <Select
    :model-value="locale"
    :options="options"
    option-label="label"
    option-value="value"
    :aria-label="t('locale.label')"
    size="small"
    @update:model-value="change"
  />
</template>
