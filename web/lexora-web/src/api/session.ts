export interface SessionBridge {
  accessToken(): string | null
  renew(): Promise<boolean>
  expire(): void
}

let bridge: SessionBridge | null = null

export function bindSession(session: SessionBridge | null): void {
  bridge = session
}

export function boundSession(): SessionBridge | null {
  return bridge
}
