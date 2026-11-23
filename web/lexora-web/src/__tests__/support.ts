import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia, type Pinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import type { Component } from 'vue'
import { createMemoryHistory, type Router } from 'vue-router'

import { createAppI18n, type AppLocale } from '@/i18n'
import { createAppRouter } from '@/router'

export interface MountOptions {
  path?: string
  locale?: AppLocale
  pinia?: Pinia
  router?: Router
  props?: Record<string, unknown>
}

export async function mountWithApp(component: Component, options: MountOptions = {}) {
  const pinia = options.pinia ?? createPinia()
  setActivePinia(pinia)
  const router = options.router ?? createAppRouter(createMemoryHistory())
  await router.push(options.path ?? '/')
  const i18n = createAppI18n(options.locale ?? 'pt-BR')

  const wrapper = mount(component, {
    props: options.props,
    global: { plugins: [pinia, i18n, router, PrimeVue, ToastService, ConfirmationService] },
  })
  await flushPromises()

  return { wrapper, router, pinia, i18n }
}
