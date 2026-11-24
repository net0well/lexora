export type RenewalOutcome = 'renewed' | 'transient' | 'rejected'

export interface SessionBridge {
  accessToken(): string | null
  renew(): Promise<RenewalOutcome>
  expire(): void
}

let bridge: SessionBridge | null = null

export function bindSession(session: SessionBridge | null): void {
  bridge = session
}

export function boundSession(): SessionBridge | null {
  return bridge
}
