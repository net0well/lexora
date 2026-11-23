import { defineStore } from 'pinia'
import { computed, shallowRef } from 'vue'

import { authApi } from '@/api/auth'
import { withCrossTabLock } from '@/api/crossTabLock'
import { bindSession, type RenewalOutcome } from '@/api/session'
import { singleFlight } from '@/api/singleFlight'
import type { AuthenticatedUser, LoginRequest, Session, UserRole } from '@/api/types/auth'
import { tokenLifetimeMs } from '@/utils/tokenLifetime'

export const renewBeforeExpiryMs = 60_000
export const retryRenewalAfterMs = 15_000
const minimumRenewalDelayMs = 5_000
const sessionChannelName = 'lexora-session'
const signedOutMessage = 'signed-out'

export type SessionEndReason = 'expired' | 'signedOut'

const roleRank: Record<UserRole, number> = { viewer: 1, member: 2, admin: 3 }

function isTransient(status: number): boolean {
  return status === 0 || status === 429 || status >= 500
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = shallowRef<string | null>(null)
  const user = shallowRef<AuthenticatedUser | null>(null)
  const isAuthenticated = computed(() => accessToken.value !== null && user.value !== null)

  let renewalTimer: ReturnType<typeof setTimeout> | undefined
  let restoration: Promise<boolean> | null = null
  let endedHandler: ((reason: SessionEndReason) => void) | null = null
  // Bumped whenever the session is dropped, so a renewal that was already running cannot bring it back.
  let epoch = 0
  const channel =
    typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(sessionChannelName)

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
    epoch++
    clearTimeout(renewalTimer)
    accessToken.value = null
    user.value = null
  }

  function end(reason: SessionEndReason): void {
    const wasAuthenticated = isAuthenticated.value
    clear()

    if (wasAuthenticated) {
      endedHandler?.(reason)
    }
  }

  function expire(): void {
    end('expired')
  }

  const renewal = singleFlight(() =>
    withCrossTabLock('lexora-refresh', async (): Promise<RenewalOutcome> => {
      const started = epoch
      const result = await authApi.refresh()

      if (started !== epoch) {
        return 'rejected'
      }

      if (result.success && result.data) {
        apply(result.data)
        return 'renewed'
      }

      return isTransient(result.status) ? 'transient' : 'rejected'
    }),
  )

  function renew(): Promise<RenewalOutcome> {
    return renewal()
  }

  async function renewInBackground(): Promise<void> {
    const started = epoch
    const outcome = await renewal()

    if (started !== epoch) {
      return
    }

    if (outcome === 'transient' && isAuthenticated.value) {
      schedule(retryRenewalAfterMs)
    } else if (outcome === 'rejected') {
      expire()
    }
  }

  function restore(): Promise<boolean> {
    restoration ??= renewal().then((outcome) => {
      if (outcome === 'transient') {
        restoration = null
      }

      return outcome === 'renewed'
    })

    return restoration
  }

  async function login(credentials: LoginRequest) {
    const result = await authApi.login(credentials)

    if (result.success && result.data) {
      apply(result.data)
    }

    return result
  }

  function announceSignOut(): void {
    channel?.postMessage(signedOutMessage)
  }

  async function logout(): Promise<void> {
    if (isAuthenticated.value) {
      await authApi.logout()
    }

    clear()
    announceSignOut()
  }

  function forget(): void {
    clear()
    announceSignOut()
  }

  function onSessionEnded(handler: (reason: SessionEndReason) => void): void {
    endedHandler = handler
  }

  function hasRole(...roles: UserRole[]): boolean {
    return user.value !== null && roles.includes(user.value.role)
  }

  function atLeast(role: UserRole): boolean {
    return user.value !== null && roleRank[user.value.role] >= roleRank[role]
  }

  channel?.addEventListener('message', (event: MessageEvent) => {
    if (event.data === signedOutMessage) {
      end('signedOut')
    }
  })

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
    forget,
    onSessionEnded,
    hasRole,
    atLeast,
  }
})
