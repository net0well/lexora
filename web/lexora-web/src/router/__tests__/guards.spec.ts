import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory } from 'vue-router'

import { authApi } from '@/api/auth'
import { session } from '@/__tests__/tokens'
import { useAuthStore } from '@/stores/auth'
import { createAppRouter } from '../index'

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
const restored = { success: true, data: session(), notifications: [], status: 200 }
const signedOut = { success: false, data: null, notifications: [], status: 401 }

describe('router guards', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('restores the session before the first page', async () => {
    api.refresh.mockResolvedValueOnce(restored)
    const router = createAppRouter(createMemoryHistory())

    await router.push('/')

    expect(router.currentRoute.value.name).toBe('knowledge-bases')
  })

  it('sends a visitor without session to the login with the way back', async () => {
    api.refresh.mockResolvedValueOnce(signedOut)
    const router = createAppRouter(createMemoryHistory())

    await router.push('/?tab=documents')

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/?tab=documents')
  })

  it('keeps a signed in user away from the login page', async () => {
    api.refresh.mockResolvedValueOnce(restored)
    const router = createAppRouter(createMemoryHistory())

    await router.push('/login')

    expect(router.currentRoute.value.name).toBe('knowledge-bases')
  })

  it('goes to the login when the session expires on a protected page', async () => {
    api.refresh.mockResolvedValueOnce(restored)
    const router = createAppRouter(createMemoryHistory())
    await router.push('/')

    useAuthStore().expire()
    await vi.waitFor(() => expect(router.currentRoute.value.name).toBe('login'))

    expect(router.currentRoute.value.query).toEqual({ redirect: '/', reason: 'expired' })
  })
})
