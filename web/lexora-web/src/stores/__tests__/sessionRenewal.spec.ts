import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest'

import { http } from '@/api/http'
import { accessToken, session } from '@/__tests__/tokens'
import { useAuthStore } from '../auth'

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json; charset=utf-8' },
  })
}

function authorizationOf(init: RequestInit | undefined): string | undefined {
  return (init?.headers as Record<string, string> | undefined)?.Authorization
}

describe('session renewal through the HTTP client', () => {
  let fetchMock: Mock<typeof fetch>
  const expired = accessToken(900, 1_700_000_000)
  const renewed = accessToken(900, 1_700_000_900)

  beforeEach(() => {
    setActivePinia(createPinia())
    fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
  })

  async function signIn(): Promise<ReturnType<typeof useAuthStore>> {
    fetchMock.mockResolvedValueOnce(
      json(200, { success: true, data: session('member', expired), notifications: [] }),
    )
    const auth = useAuthStore()
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })
    fetchMock.mockReset()

    return auth
  }

  it('renews once for concurrent requests and repeats each one with the new token', async () => {
    await signIn()
    fetchMock.mockImplementation(async (url, init) => {
      if (url === '/api/v1/auth/refresh') {
        await new Promise((resolve) => setTimeout(resolve, 10))
        return json(200, { success: true, data: session('member', renewed), notifications: [] })
      }

      return authorizationOf(init) === `Bearer ${renewed}`
        ? json(200, { success: true, data: [], notifications: [] })
        : json(401, { success: false, data: null, notifications: [] })
    })

    const results = await Promise.all([
      http.get('/knowledge-bases'),
      http.get('/conversations'),
      http.get('/ai/profiles'),
    ])

    expect(results.every((result) => result.success)).toBe(true)
    expect(fetchMock.mock.calls.filter(([url]) => url === '/api/v1/auth/refresh')).toHaveLength(1)
    expect(
      fetchMock.mock.calls.filter(([, init]) => authorizationOf(init) === `Bearer ${renewed}`),
    ).toHaveLength(3)
  })

  it('ends the session when the refresh cookie is no longer valid', async () => {
    const auth = await signIn()
    const onExpired = vi.fn()
    auth.onSessionEnded(onExpired)
    fetchMock.mockImplementation(async (url) =>
      url === '/api/v1/auth/refresh'
        ? json(401, { success: false, data: null, notifications: [] })
        : json(401, { success: false, data: null, notifications: [] }),
    )

    const result = await http.get('/knowledge-bases')

    expect(result.status).toBe(401)
    expect(auth.isAuthenticated).toBe(false)
    expect(onExpired).toHaveBeenCalledTimes(1)
  })
})
