import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'

import { apiUrl, http, parseResult } from '../http'
import { bindSession, type RenewalOutcome, type SessionBridge } from '../session'
import { singleFlight } from '../singleFlight'

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json; charset=utf-8' },
  })
}

function ok<T>(data: T): Response {
  return json(200, { success: true, data, notifications: [] })
}

const unauthorized = () =>
  json(401, {
    success: false,
    data: null,
    notifications: [
      { key: 'auth.unauthenticated', message: 'Authentication is required.', type: 'unauthorized' },
    ],
  })

function authorizationOf(call: unknown[]): string | undefined {
  const init = call[1] as RequestInit
  return (init.headers as Record<string, string>).Authorization
}

describe('http', () => {
  let fetchMock: Mock<typeof fetch>
  let token: string | null
  let session: SessionBridge & {
    renew: Mock<() => Promise<RenewalOutcome>>
    expire: Mock<() => void>
  }

  beforeEach(() => {
    fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
    token = 'old-token'
    session = {
      accessToken: () => token,
      renew: vi.fn(async () => {
        token = 'new-token'
        return 'renewed' as const
      }),
      expire: vi.fn(),
    }
    bindSession(session)
  })

  afterEach(() => {
    bindSession(null)
    vi.unstubAllGlobals()
  })

  it('sends the access token and the JSON body and reads the envelope', async () => {
    fetchMock.mockResolvedValueOnce(ok({ id: '1' }))

    const result = await http.post<{ id: string }>('/knowledge-bases', { name: 'Manuais' })

    expect(result).toEqual({ success: true, data: { id: '1' }, notifications: [], status: 200 })
    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/v1/knowledge-bases')
    expect(init).toMatchObject({
      method: 'POST',
      body: '{"name":"Manuais"}',
      credentials: 'same-origin',
    })
    expect(authorizationOf(fetchMock.mock.calls[0]!)).toBe('Bearer old-token')
  })

  it('keeps the notifications and the status of a failure', async () => {
    const notification = {
      key: 'knowledgeBase.name.duplicate',
      message: 'Duplicate.',
      type: 'conflict',
    }
    fetchMock.mockResolvedValueOnce(
      json(409, { success: false, data: null, notifications: [notification] }),
    )

    const result = await http.post('/knowledge-bases', { name: 'Manuais' })

    expect(result).toEqual({
      success: false,
      data: null,
      notifications: [notification],
      status: 409,
    })
  })

  it('turns a proxy error page into a network notification', async () => {
    fetchMock.mockResolvedValueOnce(
      new Response('<html>Bad Gateway</html>', {
        status: 502,
        headers: { 'Content-Type': 'text/html' },
      }),
    )

    const result = await http.get('/knowledge-bases')

    expect(result.success).toBe(false)
    expect(result.notifications[0]?.key).toBe('network.unavailable')
  })

  it('turns a connection failure into a network notification', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'))

    const result = await http.get('/knowledge-bases')

    expect(result).toMatchObject({ success: false, status: 0 })
    expect(result.notifications[0]?.key).toBe('network.unavailable')
  })

  it('lets an aborted request fail as an abort', async () => {
    fetchMock.mockRejectedValueOnce(new DOMException('Aborted', 'AbortError'))

    await expect(http.get('/knowledge-bases')).rejects.toMatchObject({ name: 'AbortError' })
  })

  it('renews the session once on 401 and repeats the request with the new token', async () => {
    fetchMock.mockResolvedValueOnce(unauthorized()).mockResolvedValueOnce(ok(['kb']))

    const result = await http.get<string[]>('/knowledge-bases')

    expect(result.data).toEqual(['kb'])
    expect(session.renew).toHaveBeenCalledTimes(1)
    expect(authorizationOf(fetchMock.mock.calls[1]!)).toBe('Bearer new-token')
  })

  it('renews only once for concurrent 401 responses', async () => {
    let release!: () => void
    const renewal = new Promise<void>((resolve) => (release = resolve))
    session.renew.mockImplementation(
      singleFlight(async () => {
        await renewal
        token = 'new-token'
        return 'renewed' as const
      }),
    )
    fetchMock.mockImplementation(async (_url, init) =>
      (init?.headers as Record<string, string>).Authorization === 'Bearer new-token'
        ? ok('done')
        : unauthorized(),
    )

    const pending = [http.get('/a'), http.get('/b'), http.get('/c')]
    await vi.waitFor(() => expect(session.renew).toHaveBeenCalledTimes(3))
    release()
    const results = await Promise.all(pending)

    expect(results.every((result) => result.success)).toBe(true)
    expect(
      fetchMock.mock.calls.filter((call) => authorizationOf(call) === 'Bearer new-token'),
    ).toHaveLength(3)
  })

  it('repeats without renewing when another request already renewed the token', async () => {
    fetchMock
      .mockImplementationOnce(async () => {
        token = 'renewed-meanwhile'
        return unauthorized()
      })
      .mockResolvedValueOnce(ok('done'))

    const result = await http.get('/knowledge-bases')

    expect(result.success).toBe(true)
    expect(session.renew).not.toHaveBeenCalled()
    expect(authorizationOf(fetchMock.mock.calls[1]!)).toBe('Bearer renewed-meanwhile')
  })

  it('expires the session when the renewal fails', async () => {
    session.renew.mockResolvedValueOnce('rejected')
    fetchMock.mockResolvedValueOnce(unauthorized())

    const result = await http.get('/knowledge-bases')

    expect(result.status).toBe(401)
    expect(session.expire).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('keeps the session when the renewal could not reach the server', async () => {
    session.renew.mockResolvedValueOnce('transient')
    fetchMock.mockResolvedValueOnce(unauthorized())

    const result = await http.get('/knowledge-bases')

    expect(result).toMatchObject({ success: false, status: 0 })
    expect(result.notifications[0]?.key).toBe('network.unavailable')
    expect(session.expire).not.toHaveBeenCalled()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('does not send the token nor renew for unauthenticated calls', async () => {
    fetchMock.mockResolvedValueOnce(unauthorized())

    const result = await http.post(
      '/auth/login',
      { email: 'a', password: 'b' },
      { authenticate: false },
    )

    expect(result.status).toBe(401)
    expect(authorizationOf(fetchMock.mock.calls[0]!)).toBeUndefined()
    expect(session.renew).not.toHaveBeenCalled()
  })
})

describe('apiUrl', () => {
  it('adds only the query values that are set', () => {
    expect(apiUrl('/documents', { status: 'indexed', search: '', page: 2, pageSize: null })).toBe(
      '/api/v1/documents?status=indexed&page=2',
    )
  })
})

describe('parseResult', () => {
  it('treats an empty successful response as success', () => {
    expect(parseResult(204, null, '')).toEqual({
      success: true,
      data: null,
      notifications: [],
      status: 204,
    })
  })

  it('treats a malformed JSON body as an unexpected failure', () => {
    expect(parseResult(200, 'application/json', '{').notifications[0]?.key).toBe('unexpected')
  })
})
