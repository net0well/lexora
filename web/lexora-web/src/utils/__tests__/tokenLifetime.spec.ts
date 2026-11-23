import { describe, expect, it } from 'vitest'

import { accessToken } from '@/__tests__/tokens'
import { tokenLifetimeMs } from '../tokenLifetime'

describe('tokenLifetimeMs', () => {
  it('reads the lifetime from the token times', () => {
    expect(tokenLifetimeMs(accessToken(900), '2000-01-01T00:00:00Z', Date.now())).toBe(900_000)
  })

  it('falls back to the expiry when the token cannot be read', () => {
    const receivedAt = Date.parse('2026-01-01T00:00:00Z')

    expect(tokenLifetimeMs('not-a-jwt', '2026-01-01T00:10:00Z', receivedAt)).toBe(600_000)
  })
})
