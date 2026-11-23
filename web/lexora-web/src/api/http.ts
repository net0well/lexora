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
    status === 502 || status === 503 || status === 504 ? networkUnavailable : unexpectedFailure,
  )
}

export async function readResult<T>(response: Response): Promise<ApiResult<T>> {
  return parseResult<T>(
    response.status,
    response.headers.get('content-type'),
    await response.text(),
  )
}

interface Attempt<T> {
  result: ApiResult<T>
  token: string | null
}

async function send<T>(method: string, path: string, options: RequestOptions): Promise<Attempt<T>> {
  const token = options.authenticate === false ? null : (boundSession()?.accessToken() ?? null)
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

    return { result: await readResult<T>(response), token }
  } catch (error) {
    if (isAbort(error)) {
      throw error
    }

    return { result: failure(0, networkUnavailable), token }
  }
}

export async function request<T>(
  method: string,
  path: string,
  options: RequestOptions = {},
): Promise<ApiResult<T>> {
  const first = await send<T>(method, path, options)
  const session = boundSession()

  if (first.result.status !== 401 || options.authenticate === false || !session) {
    return first.result
  }

  const current = session.accessToken()
  const renewedElsewhere = current !== null && current !== first.token

  if (!renewedElsewhere && !(await session.renew())) {
    session.expire()

    return first.result
  }

  return (await send<T>(method, path, options)).result
}

export const http = {
  get: <T>(path: string, options?: RequestOptions) => request<T>('GET', path, options),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>('POST', path, { ...options, body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>('PUT', path, { ...options, body }),
  delete: <T>(path: string, options?: RequestOptions) => request<T>('DELETE', path, options),
}
