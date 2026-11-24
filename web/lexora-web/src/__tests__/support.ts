import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia, type Pinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import { defineComponent, type Component } from 'vue'
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
    attachTo: document.body,
    global: { plugins: [pinia, i18n, router, PrimeVue, ToastService, ConfirmationService] },
  })
  await flushPromises()

  return { wrapper, router, pinia, i18n }
}

export function buttonByText(root: ParentNode, text: string): HTMLButtonElement | undefined {
  return [...root.querySelectorAll<HTMLButtonElement>('button')].find(
    (button) => button.textContent?.trim() === text,
  )
}

export function clickText(root: ParentNode, text: string): void {
  const target = [...root.querySelectorAll<HTMLElement>('*')]
    .filter((element) => element.textContent?.trim() === text)
    .pop()

  if (!target) {
    throw new Error(`Nothing shows the text "${text}".`)
  }

  target.click()
}

export function withSetup<T>(composable: () => T): { result: T; unmount: () => void } {
  let result!: T
  const wrapper = mount(
    defineComponent({
      setup() {
        result = composable()
        return () => null
      },
    }),
  )

  return { result, unmount: () => wrapper.unmount() }
}
