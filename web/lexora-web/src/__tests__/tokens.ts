import type { Session, UserRole } from '@/api/types/auth'

function base64Url(value: unknown): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

export function accessToken(lifetimeSeconds = 900, issuedAt = 1_700_000_000): string {
  return `${base64Url({ alg: 'HS256', typ: 'JWT' })}.${base64Url({ iat: issuedAt, exp: issuedAt + lifetimeSeconds })}.signature`
}

export function session(role: UserRole = 'admin', token = accessToken()): Session {
  return {
    accessToken: token,
    expiresAt: new Date(Date.now() + 900_000).toISOString(),
    user: { id: 'user-1', email: 'ana@lexora.local', name: 'Ana', role },
  }
}
