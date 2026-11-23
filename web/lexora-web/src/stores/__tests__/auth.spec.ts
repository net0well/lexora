import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { authApi } from '@/api/auth'
import { boundSession } from '@/api/session'
import type { ApiResult } from '@/api/types/commandResult'
import type { Session } from '@/api/types/auth'
import { accessToken, session } from '@/__tests__/tokens'
import { renewBeforeExpiryMs, retryRenewalAfterMs, useAuthStore } from '../auth'

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

function succeeded(data: Session): ApiResult<Session> {
  return { success: true, data, notifications: [], status: 200 }
}

function failed(status: number, key = 'auth.refreshToken.invalid'): ApiResult<Session> {
  return {
    success: false,
    data: null,
    notifications: [{ key, message: key, type: 'unauthorized' }],
    status,
  }
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('keeps the session of a successful login and lends its token to the HTTP client', async () => {
    const signedIn = session('member')
    api.login.mockResolvedValueOnce(succeeded(signedIn))
    const auth = useAuthStore()

    const result = await auth.login({ email: 'ana@lexora.local', password: 'secret' })

    expect(result.success).toBe(true)
    expect(auth.isAuthenticated).toBe(true)
    expect(auth.user?.role).toBe('member')
    expect(boundSession()?.accessToken()).toBe(signedIn.accessToken)
  })

  it('stays signed out after a failed login', async () => {
    api.login.mockResolvedValueOnce(failed(401, 'auth.invalidCredentials'))
    const auth = useAuthStore()

    const result = await auth.login({ email: 'ana@lexora.local', password: 'wrong' })

    expect(result.notifications[0]?.key).toBe('auth.invalidCredentials')
    expect(auth.isAuthenticated).toBe(false)
  })

  it('restores the session from the refresh cookie only once', async () => {
    api.refresh.mockResolvedValue(succeeded(session()))
    const auth = useAuthStore()

    const restored = await Promise.all([auth.restore(), auth.restore()])
    await auth.restore()

    expect(restored).toEqual([true, true])
    expect(api.refresh).toHaveBeenCalledTimes(1)
    expect(auth.isAuthenticated).toBe(true)
  })

  it('renews once for concurrent requests', async () => {
    api.refresh.mockResolvedValue(succeeded(session()))
    const auth = useAuthStore()

    await Promise.all([auth.renew(), auth.renew(), auth.renew()])

    expect(api.refresh).toHaveBeenCalledTimes(1)
  })

  it('renews one minute before the token expires', async () => {
    vi.useFakeTimers()
    api.login.mockResolvedValueOnce(succeeded(session('admin', accessToken(900))))
    api.refresh.mockResolvedValue(succeeded(session()))
    const auth = useAuthStore()
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })

    await vi.advanceTimersByTimeAsync(900_000 - renewBeforeExpiryMs - 1_000)
    expect(api.refresh).not.toHaveBeenCalled()

    await vi.advanceTimersByTimeAsync(2_000)
    expect(api.refresh).toHaveBeenCalledTimes(1)
  })

  it('schedules from the token lifetime even when the client clock is far ahead', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2030-01-01T00:00:00Z'))
    const stale = { ...session('admin', accessToken(900)), expiresAt: '2026-01-01T00:15:00Z' }
    api.login.mockResolvedValueOnce(succeeded(stale))
    api.refresh.mockResolvedValue(succeeded(session()))
    const auth = useAuthStore()
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })

    await vi.advanceTimersByTimeAsync(60_000)

    expect(api.refresh).not.toHaveBeenCalled()
  })

  it('keeps the session and retries when the renewal fails for a transient reason', async () => {
    vi.useFakeTimers()
    api.login.mockResolvedValueOnce(succeeded(session('admin', accessToken(120))))
    api.refresh
      .mockResolvedValueOnce(failed(0, 'network.unavailable'))
      .mockResolvedValueOnce(succeeded(session()))
    const auth = useAuthStore()
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })

    await vi.advanceTimersByTimeAsync(60_000)
    expect(auth.isAuthenticated).toBe(true)

    await vi.advanceTimersByTimeAsync(retryRenewalAfterMs)
    expect(api.refresh).toHaveBeenCalledTimes(2)
    expect(auth.isAuthenticated).toBe(true)
  })

  it('ends the session and warns when the renewal is rejected', async () => {
    vi.useFakeTimers()
    api.login.mockResolvedValueOnce(succeeded(session('admin', accessToken(120))))
    api.refresh.mockResolvedValueOnce(failed(401))
    const expired = vi.fn()
    const auth = useAuthStore()
    auth.onExpired(expired)
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })

    await vi.advanceTimersByTimeAsync(60_000)

    expect(auth.isAuthenticated).toBe(false)
    expect(expired).toHaveBeenCalledTimes(1)
  })

  it('signs out on the server and forgets the session', async () => {
    api.login.mockResolvedValueOnce(succeeded(session()))
    api.logout.mockResolvedValueOnce({ success: true, data: null, notifications: [], status: 200 })
    const auth = useAuthStore()
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })

    await auth.logout()

    expect(api.logout).toHaveBeenCalledTimes(1)
    expect(auth.isAuthenticated).toBe(false)
    expect(boundSession()?.accessToken()).toBeNull()
  })

  it('compares roles by rank', async () => {
    api.login.mockResolvedValueOnce(succeeded(session('member')))
    const auth = useAuthStore()
    await auth.login({ email: 'ana@lexora.local', password: 'secret' })

    expect([auth.atLeast('viewer'), auth.atLeast('member'), auth.atLeast('admin')]).toEqual([
      true,
      true,
      false,
    ])
    expect(auth.hasRole('admin', 'member')).toBe(true)
  })
})
