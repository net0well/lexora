import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter, type RouteRecordRaw } from 'vue-router'

import { authApi } from '@/api/auth'
import type { UserRole } from '@/api/types/auth'
import { session } from '@/__tests__/tokens'
import { installGuards } from '../guards'

vi.mock('@/api/auth', () => ({
  authApi: {
    login: vi.fn(),
    refresh: vi.fn(),
    logout: vi.fn(),
    me: vi.fn(),
    changePassword: vi.fn(),
  },
}))

const page = { render: () => null }

const routes: RouteRecordRaw[] = [
  { path: '/login', name: 'login', component: page, meta: { guestOnly: true } },
  {
    path: '/',
    component: page,
    meta: { requiresAuth: true },
    children: [
      { path: '', name: 'knowledge-bases', component: page },
      { path: 'admin/users', name: 'admin-users', component: page, meta: { roles: ['admin'] } },
    ],
  },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: page },
]

async function routerFor(role: UserRole) {
  vi.mocked(authApi).refresh.mockResolvedValueOnce({
    success: true,
    data: session(role),
    notifications: [],
    status: 200,
  })
  const router = createRouter({ history: createMemoryHistory(), routes })
  installGuards(router)

  return router
}

describe('role guards', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('lets an admin open an admin page', async () => {
    const router = await routerFor('admin')

    await router.push('/admin/users')

    expect(router.currentRoute.value.name).toBe('admin-users')
  })

  it.each<UserRole>(['member', 'viewer'])(
    'shows the not found page to a %s on an admin page',
    async (role) => {
      const router = await routerFor(role)

      await router.push('/admin/users?page=2')

      expect(router.currentRoute.value.name).toBe('not-found')
      expect(router.currentRoute.value.fullPath).toBe('/admin/users?page=2')
    },
  )

  it('applies the authentication of the parent route to its children', async () => {
    vi.mocked(authApi).refresh.mockResolvedValueOnce({
      success: false,
      data: null,
      notifications: [],
      status: 401,
    })
    const router = createRouter({ history: createMemoryHistory(), routes })
    installGuards(router)

    await router.push('/admin/users')

    expect(router.currentRoute.value.name).toBe('login')
  })
})
