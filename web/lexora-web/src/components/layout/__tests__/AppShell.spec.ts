import { beforeEach, describe, expect, it, vi } from 'vitest'

import App from '@/App.vue'
import { authApi } from '@/api/auth'
import { clickText, mountWithApp } from '@/__tests__/support'
import { session } from '@/__tests__/tokens'

vi.mock('@/api/auth', () => ({
  authApi: {
    login: vi.fn(),
    refresh: vi.fn(),
    logout: vi.fn(),
    me: vi.fn(),
    changePassword: vi.fn(),
  },
}))

const api = vi.mocked(authApi)

describe('AppShell', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    const signedIn = session('member')
    signedIn.user.name = 'Ana Souza'
    api.refresh.mockResolvedValue({ success: true, data: signedIn, notifications: [], status: 200 })
    api.logout.mockResolvedValue({ success: true, data: null, notifications: [], status: 200 })
  })

  it('shows the signed in user and the navigation', async () => {
    const { wrapper } = await mountWithApp(App, { path: '/' })

    expect(wrapper.find('button[aria-label="Menu do usuário"]').text()).toContain('Ana Souza')
    expect(wrapper.find('button[aria-label="Menu do usuário"]').text()).toContain('AS')
    expect(wrapper.find('nav').text()).toContain('Bases de conhecimento')
  })

  it('signs out from the user menu', async () => {
    const { wrapper, router } = await mountWithApp(App, { path: '/' })

    await wrapper.find('button[aria-label="Menu do usuário"]').trigger('click')
    clickText(document.body, 'Sair')

    await vi.waitFor(() => expect(router.currentRoute.value.name).toBe('login'))
    expect(api.logout).toHaveBeenCalledTimes(1)
  })
})
