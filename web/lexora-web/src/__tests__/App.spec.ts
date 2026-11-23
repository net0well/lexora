import { describe, expect, it } from 'vitest'
import { defineComponent, nextTick, onMounted } from 'vue'
import { useConfirm } from 'primevue/useconfirm'
import { createMemoryHistory, createRouter } from 'vue-router'

import App from '../App.vue'
import { buttonByText, mountWithApp } from './support'

const AsksForConfirmation = defineComponent({
  setup() {
    const confirm = useConfirm()
    onMounted(() => confirm.require({ message: 'Arquivar a base?' }))

    return () => null
  },
})

describe('App', () => {
  it('shows the login page to a visitor without session', async () => {
    const { wrapper, router } = await mountWithApp(App, { path: '/' })

    expect(router.currentRoute.value.name).toBe('login')
    expect(wrapper.find('form').exists()).toBe(true)
  })

  it('shows the not found page in the chosen language', async () => {
    const { wrapper, router } = await mountWithApp(App, { path: '/does/not/exist', locale: 'en' })

    expect(router.currentRoute.value.name).toBe('not-found')
    expect(wrapper.text()).toContain('Page not found')
  })

  it('switches the page and the components to the chosen language', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/', component: AsksForConfirmation }],
    })
    const { i18n } = await mountWithApp(App, { router, locale: 'pt-BR' })

    expect(document.documentElement.lang).toBe('pt-BR')
    expect(buttonByText(document.body, 'Sim')).toBeDefined()

    i18n.global.locale.value = 'en'
    await nextTick()

    expect(document.documentElement.lang).toBe('en')
    expect(buttonByText(document.body, 'Yes')).toBeDefined()
  })
})
