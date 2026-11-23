interface TokenTimes {
  iat: number
  exp: number
}

function readTimes(accessToken: string): TokenTimes | null {
  try {
    const payload = accessToken.split('.')[1] ?? ''
    const claims = JSON.parse(
      atob(payload.replace(/-/g, '+').replace(/_/g, '/')),
    ) as Partial<TokenTimes>

    return typeof claims.iat === 'number' &&
      typeof claims.exp === 'number' &&
      claims.exp > claims.iat
      ? { iat: claims.iat, exp: claims.exp }
      : null
  } catch {
    return null
  }
}

// The renewal is scheduled from the token lifetime (exp - iat) counted from when it arrived, so a client
// clock that is off from the server does not renew too late or in a loop.
export function tokenLifetimeMs(
  accessToken: string,
  expiresAt: string,
  receivedAt: number,
): number {
  const times = readTimes(accessToken)

  return times ? (times.exp - times.iat) * 1000 : Math.max(Date.parse(expiresAt) - receivedAt, 0)
}
