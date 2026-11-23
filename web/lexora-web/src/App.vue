<script setup lang="ts">
import { watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { usePrimeVue } from 'primevue/config'
import ConfirmDialog from 'primevue/confirmdialog'
import Toast from 'primevue/toast'

import { isSupportedLocale } from '@/i18n'
import { primeVueLocales } from '@/i18n/primeVueLocales'

const { locale } = useI18n()
const primevue = usePrimeVue()

watch(
  locale,
  (value) => {
    if (isSupportedLocale(value)) {
      Object.assign(primevue.config.locale ?? {}, primeVueLocales[value])
      document.documentElement.lang = value
    }
  },
  { immediate: true },
)
</script>

<template>
  <Toast position="top-right" />
  <ConfirmDialog />
  <RouterView />
</template>
