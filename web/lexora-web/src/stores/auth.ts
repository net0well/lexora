import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { authApi } from '@/api/auth'
import { withCrossTabLock } from '@/api/crossTabLock'
import { bindSession } from '@/api/session'
import { singleFlight } from '@/api/singleFlight'
import type { AuthenticatedUser, LoginRequest, Session, UserRole } from '@/api/types/auth'
import { tokenLifetimeMs } from './tokenLifetime'

export const renewBeforeExpiryMs = 60_000
export const retryRenewalAfterMs = 15_000
const minimumRenewalDelayMs = 5_000

const roleRank: Record<UserRole, number> = { viewer: 1, member: 2, admin: 3 }

type Renewal = 'renewed' | 'transient' | 'rejected'

function isTransient(status: number): boolean {
  return status === 0 || status === 429 || status >= 500
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const user = ref<AuthenticatedUser | null>(null)
  const isAuthenticated = computed(() => accessToken.value !== null && user.value !== null)

  let renewalTimer: ReturnType<typeof setTimeout> | undefined
  let restoration: Promise<boolean> | null = null
  let expiredHandler: (() => void) | null = null

  function schedule(delayMs: number): void {
    clearTimeout(renewalTimer)
    renewalTimer = setTimeout(
      () => void renewInBackground(),
      Math.max(delayMs, minimumRenewalDelayMs),
    )
  }

  function apply(session: Session): void {
    accessToken.value = session.accessToken
    user.value = session.user
    schedule(
      tokenLifetimeMs(session.accessToken, session.expiresAt, Date.now()) - renewBeforeExpiryMs,
    )
  }

  function clear(): void {
    clearTimeout(renewalTimer)
    accessToken.value = null
    user.value = null
  }

  function expire(): void {
    const wasAuthenticated = isAuthenticated.value
    clear()

    if (wasAuthenticated) {
      expiredHandler?.()
    }
  }

  const renewal = singleFlight(() =>
    withCrossTabLock('lexora-refresh', async (): Promise<Renewal> => {
      const result = await authApi.refresh()

      if (result.success && result.data) {
        apply(result.data)
        return 'renewed'
      }

      return isTransient(result.status) ? 'transient' : 'rejected'
    }),
  )

  async function renew(): Promise<boolean> {
    return (await renewal()) === 'renewed'
  }

  async function renewInBackground(): Promise<void> {
    const outcome = await renewal()

    if (outcome === 'transient' && isAuthenticated.value) {
      schedule(retryRenewalAfterMs)
    } else if (outcome === 'rejected') {
      expire()
    }
  }

  function restore(): Promise<boolean> {
    restoration ??= renew()

    return restoration
  }

  async function login(credentials: LoginRequest) {
    const result = await authApi.login(credentials)

    if (result.success && result.data) {
      apply(result.data)
    }

    return result
  }

  async function logout(): Promise<void> {
    if (isAuthenticated.value) {
      await authApi.logout()
    }

    clear()
  }

  function onExpired(handler: () => void): void {
    expiredHandler = handler
  }

  function hasRole(...roles: UserRole[]): boolean {
    return user.value !== null && roles.includes(user.value.role)
  }

  function atLeast(role: UserRole): boolean {
    return user.value !== null && roleRank[user.value.role] >= roleRank[role]
  }

  bindSession({ accessToken: () => accessToken.value, renew, expire })

  return {
    accessToken,
    user,
    isAuthenticated,
    login,
    logout,
    restore,
    renew,
    expire,
    onExpired,
    hasRole,
    atLeast,
  }
})
