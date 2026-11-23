import { describe, expect, it } from 'vitest'

import App from '../App.vue'
import { mountWithApp } from './support'

describe('App', () => {
  it('renders the route of the current path', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/' })

    expect(wrapper.text()).toContain('Lexora')
  })

  it('shows the not found page in the chosen language', async () => {
    const { wrapper, router } = await mountWithApp(App, { path: '/does/not/exist', locale: 'en' })

    expect(router.currentRoute.value.name).toBe('not-found')
    expect(wrapper.text()).toContain('Page not found')
  })

  it('sets the document and component language from the locale', async () => {
    const { wrapper, i18n } = await mountWithApp(App, { locale: 'pt-BR' })

    expect(document.documentElement.lang).toBe('pt-BR')
    expect(wrapper.vm.$primevue.config.locale?.accept).toBe('Sim')

    i18n.global.locale.value = 'en'
    await wrapper.vm.$nextTick()

    expect(document.documentElement.lang).toBe('en')
    expect(wrapper.vm.$primevue.config.locale?.accept).toBe('Yes')
  })
})
