import { describe, expect, it } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import { createMemoryHistory } from 'vue-router'

import App from '../App.vue'
import { createAppRouter } from '../router'

async function mountAt(path: string) {
  const router = createAppRouter(createMemoryHistory())
  await router.push(path)
  const wrapper = mount(App, {
    global: { plugins: [createPinia(), router, PrimeVue, ToastService, ConfirmationService] },
  })
  await flushPromises()

  return { wrapper, router }
}

describe('App', () => {
  it('renders the route of the current path', async () => {
    const { wrapper } = await mountAt('/')

    expect(wrapper.text()).toContain('Lexora')
  })

  it('shows the not found page for an unknown path', async () => {
    const { wrapper, router } = await mountAt('/does/not/exist')

    expect(router.currentRoute.value.name).toBe('not-found')
    expect(wrapper.text()).toContain('404')
  })
})
