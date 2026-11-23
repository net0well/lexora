import { boundSession } from './session'
import type { ApiNotification, ApiResult, CommandResult } from './types/commandResult'

export const apiBase = '/api/v1'

export type QueryValue = string | number | boolean | null | undefined

export interface RequestOptions {
  body?: unknown
  query?: Record<string, QueryValue>
  signal?: AbortSignal
  authenticate?: boolean
}

export const networkUnavailable: ApiNotification = {
  key: 'network.unavailable',
  message: 'Could not reach the server.',
  type: 'unavailable',
}

export const unexpectedFailure: ApiNotification = {
  key: 'unexpected',
  message: 'An unexpected error occurred.',
  type: 'businessRule',
}

export function apiUrl(path: string, query?: Record<string, QueryValue>): string {
  const parameters = new URLSearchParams()

  for (const [name, value] of Object.entries(query ?? {})) {
    if (value !== null && value !== undefined && value !== '') {
      parameters.set(name, String(value))
    }
  }

  const search = parameters.toString()

  return `${apiBase}${path}${search ? `?${search}` : ''}`
}

export function failure<T>(status: number, notification: ApiNotification): ApiResult<T> {
  return { success: false, data: null, notifications: [notification], status }
}

export function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}

export function parseResult<T>(
  status: number,
  contentType: string | null,
  text: string,
): ApiResult<T> {
  const ok = status >= 200 && status < 300

  if (contentType?.includes('json') && text) {
    try {
      const body = JSON.parse(text) as Partial<CommandResult<T>>

      if (typeof body.success === 'boolean') {
        return {
          success: body.success && ok,
          data: body.data ?? null,
          notifications: body.notifications ?? [],
          status,
        }
      }
    } catch {
      return failure(status, unexpectedFailure)
    }
  }

  if (ok) {
    return { success: true, data: null, notifications: [], status }
  }

  return failure(
    status,
    [0, 502, 503, 504].includes(status) ? networkUnavailable : unexpectedFailure,
  )
}

export async function readResult<T>(response: Response): Promise<ApiResult<T>> {
  return parseResult<T>(
    response.status,
    response.headers.get('content-type'),
    await response.text(),
  )
}

export async function withSessionRetry<T>(
  attempt: (accessToken: string | null) => Promise<ApiResult<T>>,
): Promise<ApiResult<T>> {
  const session = boundSession()
  const token = session?.accessToken() ?? null
  const first = await attempt(token)

  if (first.status !== 401 || !session) {
    return first
  }

  const current = session.accessToken()
  const renewedElsewhere = current !== null && current !== token

  if (!renewedElsewhere) {
    const outcome = await session.renew()

    // Losing the connection while renewing does not mean the session ended: keep it and let the
    // caller report a network problem.
    if (outcome === 'transient') {
      return failure(0, networkUnavailable)
    }

    if (outcome === 'rejected') {
      session.expire()

      return first
    }
  }

  return attempt(session.accessToken())
}

async function send<T>(
  method: string,
  path: string,
  options: RequestOptions,
  token: string | null,
): Promise<ApiResult<T>> {
  const headers: Record<string, string> = { Accept: 'application/json' }

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  try {
    const response = await fetch(apiUrl(path, options.query), {
      method,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      credentials: 'same-origin',
      signal: options.signal,
    })

    return await readResult<T>(response)
  } catch (error) {
    if (isAbort(error)) {
      throw error
    }

    return failure(0, networkUnavailable)
  }
}

export function request<T>(
  method: string,
  path: string,
  options: RequestOptions = {},
): Promise<ApiResult<T>> {
  return options.authenticate === false
    ? send<T>(method, path, options, null)
    : withSessionRetry((token) => send<T>(method, path, options, token))
}

export const http = {
  get: <T>(path: string, options?: RequestOptions) => request<T>('GET', path, options),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>('POST', path, { ...options, body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>('PUT', path, { ...options, body }),
  delete: <T>(path: string, options?: RequestOptions) => request<T>('DELETE', path, options),
}
