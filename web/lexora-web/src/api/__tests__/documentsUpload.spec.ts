import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { documentsApi } from '../documents'
import { bindSession, type SessionBridge } from '../session'

interface Reply {
  status: number
  body?: unknown
  progress?: number[]
  fail?: boolean
}

class FakeXhr {
  static replies: Reply[] = []
  static sent: FakeXhr[] = []

  headers: Record<string, string> = {}
  method = ''
  url = ''
  body: FormData | null = null
  status = 0
  responseText = ''
  upload: { onprogress: ((event: ProgressEvent) => void) | null } = { onprogress: null }
  onload: (() => void) | null = null
  onerror: (() => void) | null = null
  onabort: (() => void) | null = null

  open(method: string, url: string): void {
    this.method = method
    this.url = url
  }

  setRequestHeader(name: string, value: string): void {
    this.headers[name] = value
  }

  getResponseHeader(): string {
    return 'application/json; charset=utf-8'
  }

  abort(): void {
    this.onabort?.()
  }

  send(body: FormData): void {
    this.body = body
    FakeXhr.sent.push(this)
    const reply = FakeXhr.replies.shift()

    if (!reply) {
      return
    }

    queueMicrotask(() => {
      for (const loaded of reply.progress ?? []) {
        this.upload.onprogress?.({ lengthComputable: true, loaded, total: 100 } as ProgressEvent)
      }

      if (reply.fail) {
        this.onerror?.()
        return
      }

      this.status = reply.status
      this.responseText = JSON.stringify(reply.body)
      this.onload?.()
    })
  }
}

const uploaded = {
  id: 'doc-1',
  fileName: 'manual.pdf',
  format: 'pdf',
  status: 'uploaded',
  sizeBytes: 3,
}
const file = new File(['pdf'], 'manual.pdf', { type: 'application/pdf' })

describe('documentsApi.upload', () => {
  let token: string
  let session: SessionBridge

  beforeEach(() => {
    FakeXhr.replies = []
    FakeXhr.sent = []
    vi.stubGlobal('XMLHttpRequest', FakeXhr)
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
  })

  it('sends the file with the token and reports the progress', async () => {
    FakeXhr.replies.push({
      status: 202,
      progress: [50, 100],
      body: { success: true, data: uploaded, notifications: [] },
    })
    const progress: number[] = []

    const result = await documentsApi.upload('kb-1', file, {
      onProgress: (value) => progress.push(value),
    })

    expect(result).toMatchObject({ success: true, data: uploaded, status: 202 })
    expect(progress).toEqual([0, 0.5, 1])
    const request = FakeXhr.sent[0]!
    expect(request.method).toBe('POST')
    expect(request.url).toBe('/api/v1/knowledge-bases/kb-1/documents')
    expect(request.headers.Authorization).toBe('Bearer old-token')
    expect((request.body?.get('file') as File).name).toBe('manual.pdf')
  })

  it('renews the session and sends the file again after a 401', async () => {
    FakeXhr.replies.push(
      { status: 401, body: { success: false, data: null, notifications: [] } },
      { status: 202, body: { success: true, data: uploaded, notifications: [] } },
    )

    const result = await documentsApi.upload('kb-1', file)

    expect(result.success).toBe(true)
    expect(session.renew).toHaveBeenCalledTimes(1)
    expect(FakeXhr.sent.map((request) => request.headers.Authorization)).toEqual([
      'Bearer old-token',
      'Bearer new-token',
    ])
  })

  it('keeps the reason the server refused the file', async () => {
    const duplicate = { key: 'document.duplicate', message: 'Duplicate.', type: 'conflict' }
    FakeXhr.replies.push({
      status: 409,
      body: { success: false, data: null, notifications: [duplicate] },
    })

    const result = await documentsApi.upload('kb-1', file)

    expect(result).toEqual({ success: false, data: null, notifications: [duplicate], status: 409 })
  })

  it('reports a lost connection as a network problem', async () => {
    FakeXhr.replies.push({ status: 0, fail: true })

    const result = await documentsApi.upload('kb-1', file)

    expect(result.notifications[0]?.key).toBe('network.unavailable')
  })

  it('fails as an abort when the upload is cancelled', async () => {
    const controller = new AbortController()
    const pending = documentsApi.upload('kb-1', file, { signal: controller.signal })

    controller.abort()

    await expect(pending).rejects.toMatchObject({ name: 'AbortError' })
  })

  it('does not send the file again when it was cancelled during the renewal', async () => {
    const controller = new AbortController()
    vi.mocked(session.renew).mockImplementationOnce(async () => {
      controller.abort()
      token = 'new-token'
      return 'renewed'
    })
    FakeXhr.replies.push({ status: 401, body: { success: false, data: null, notifications: [] } })

    const pending = documentsApi.upload('kb-1', file, { signal: controller.signal })

    await expect(pending).rejects.toMatchObject({ name: 'AbortError' })
    expect(FakeXhr.sent).toHaveLength(1)
  })
})
