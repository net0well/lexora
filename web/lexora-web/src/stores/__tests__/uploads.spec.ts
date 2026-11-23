import { flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { authApi } from '@/api/auth'
import { documentsApi, type UploadOptions } from '@/api/documents'
import type { ApiNotification, ApiResult } from '@/api/types/commandResult'
import type { UploadedDocument } from '@/api/types/document'
import { ok, sessionAs } from '@/__tests__/tokens'
import { useAuthStore } from '../auth'
import { useUploadsStore } from '../uploads'

vi.mock('@/api/auth', () => ({
  authApi: { login: vi.fn(), refresh: vi.fn(), logout: vi.fn() },
}))
vi.mock('@/api/documents', () => ({ documentsApi: { upload: vi.fn() } }))

const upload = vi.mocked(documentsApi.upload)

interface Sending {
  file: File
  signal: AbortSignal | undefined
  finish: (result: ApiResult<UploadedDocument>) => void
}

function holdUploads(): Sending[] {
  const sending: Sending[] = []
  upload.mockImplementation(
    (_id: string, file: File, options: UploadOptions = {}) =>
      new Promise((resolve, reject) => {
        options.signal?.addEventListener('abort', () =>
          reject(new DOMException('Cancelled', 'AbortError')),
        )
        sending.push({ file, signal: options.signal, finish: resolve })
      }),
  )

  return sending
}

function stored(file: File): ApiResult<UploadedDocument> {
  return ok(
    { id: file.name, fileName: file.name, format: 'pdf', status: 'uploaded', sizeBytes: 3 },
    202,
  )
}

const files = ['a.pdf', 'b.pdf', 'c.pdf'].map((name) => new File(['pdf'], name))

describe('uploads store', () => {
  beforeEach(async () => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(authApi.login).mockResolvedValue(sessionAs('member'))
    await useAuthStore().login({ email: 'ana@lexora.local', password: 'secret' })
  })

  it('sends two files at a time and reports each one that was stored', async () => {
    const sending = holdUploads()
    const uploaded = vi.fn()
    const uploads = useUploadsStore()

    uploads.add('kb-1', files, uploaded)
    expect(upload).toHaveBeenCalledTimes(2)

    sending[0]!.finish(stored(sending[0]!.file))
    await flushPromises()
    expect(upload).toHaveBeenCalledTimes(3)

    sending[1]!.finish(stored(sending[1]!.file))
    sending[2]!.finish(stored(sending[2]!.file))
    await flushPromises()

    expect(uploads.itemsFor('kb-1').map((item) => item.state)).toEqual(['done', 'done', 'done'])
    expect(uploaded.mock.calls.map(([document, baseId]) => [document.id, baseId])).toEqual([
      ['a.pdf', 'kb-1'],
      ['b.pdf', 'kb-1'],
      ['c.pdf', 'kb-1'],
    ])
  })

  it('shows a refused file without sending it', () => {
    holdUploads()
    const uploads = useUploadsStore()

    uploads.add('kb-1', [new File(['png'], 'foto.png')], vi.fn())

    expect(upload).not.toHaveBeenCalled()
    expect(uploads.itemsFor('kb-1')[0]).toMatchObject({ state: 'rejected', rejection: 'format' })
  })

  it('keeps the reason the server refused a file', async () => {
    const duplicate: ApiNotification = {
      key: 'document.duplicate',
      message: 'Duplicate.',
      type: 'conflict',
    }
    upload.mockResolvedValueOnce({
      success: false,
      data: null,
      notifications: [duplicate],
      status: 409,
    })
    const uploads = useUploadsStore()

    uploads.add('kb-1', [files[0]!], vi.fn())
    await flushPromises()

    expect(uploads.itemsFor('kb-1')[0]).toMatchObject({
      state: 'failed',
      notifications: [duplicate],
    })
  })

  it('cancels a waiting file before it is sent and a running one while it is sent', async () => {
    const sending = holdUploads()
    const uploads = useUploadsStore()
    uploads.add('kb-1', files, vi.fn())
    const [running, , waiting] = uploads.itemsFor('kb-1')

    uploads.cancel('kb-1', waiting!.key)
    uploads.cancel('kb-1', running!.key)
    await flushPromises()

    expect(sending[0]!.signal?.aborted).toBe(true)
    expect(uploads.itemsFor('kb-1').map((item) => item.name)).toEqual(['b.pdf'])
    expect(upload).toHaveBeenCalledTimes(2)
  })

  it('clears the finished files and keeps the ones still being sent', async () => {
    const sending = holdUploads()
    const uploads = useUploadsStore()
    uploads.add('kb-1', files.slice(0, 2), vi.fn())

    sending[0]!.finish(stored(sending[0]!.file))
    await flushPromises()
    uploads.clearFinished('kb-1')

    expect(uploads.itemsFor('kb-1').map((item) => item.name)).toEqual(['b.pdf'])
  })

  it('stops and forgets the uploads when the user signs out', async () => {
    const sending = holdUploads()
    const uploads = useUploadsStore()
    uploads.add('kb-1', files, vi.fn())

    useAuthStore().forget()
    await flushPromises()

    expect(sending.every((entry) => entry.signal?.aborted)).toBe(true)
    expect(uploads.itemsFor('kb-1')).toEqual([])
    expect(upload).toHaveBeenCalledTimes(2)
  })
})
