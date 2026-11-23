import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'

import { documentsApi } from '@/api/documents'
import type { ApiResult } from '@/api/types/commandResult'
import type { UploadedDocument } from '@/api/types/document'
import { mountWithApp } from '@/__tests__/support'
import { ok } from '@/__tests__/tokens'
import { rejectionOf, useUpload } from '../useUpload'

vi.mock('@/api/documents', () => ({ documentsApi: { upload: vi.fn() } }))

const api = vi.mocked(documentsApi)

function pdf(name: string, size = 10): File {
  return new File(['x'.repeat(size)], name, { type: 'application/pdf' })
}

async function uploader() {
  const onUploaded = vi.fn()
  let upload!: ReturnType<typeof useUpload>
  await mountWithApp(
    defineComponent({
      setup() {
        upload = useUpload({ knowledgeBaseId: 'kb-1', onUploaded })
        return () => null
      },
    }),
    { path: '/login' },
  )

  return { upload, onUploaded }
}

function uploadedAs(name: string): ApiResult<UploadedDocument> {
  return ok({ id: name, fileName: name, format: 'pdf', status: 'uploaded', sizeBytes: 10 }, 202)
}

describe('rejectionOf', () => {
  it.each([
    ['manual.PDF', 10, null],
    ['notes.markdown', 10, null],
    ['photo.png', 10, 'format'],
    ['empty.txt', 0, 'empty'],
    ['huge.pdf', 52_428_801, 'size'],
  ])('judges %s with %i bytes as %s', (name, size, expected) => {
    const file = new File([], name)
    Object.defineProperty(file, 'size', { value: size })

    expect(rejectionOf(file)).toBe(expected)
  })
})

describe('useUpload', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('refuses a file the server would not accept without sending it', async () => {
    const { upload } = await uploader()

    upload.add([new File(['x'], 'photo.png')])

    expect(upload.items.value[0]).toMatchObject({ state: 'rejected' })
    expect(upload.items.value[0]?.error).toContain('Formato não aceito')
    expect(api.upload).not.toHaveBeenCalled()
  })

  it('sends two files at a time and reports each one uploaded', async () => {
    const releases: (() => void)[] = []
    api.upload.mockImplementation(
      (_id, file) => new Promise((resolve) => releases.push(() => resolve(uploadedAs(file.name)))),
    )
    const { upload, onUploaded } = await uploader()

    upload.add([pdf('a.pdf'), pdf('b.pdf'), pdf('c.pdf')])
    await flushPromises()

    expect(api.upload).toHaveBeenCalledTimes(2)
    expect(upload.items.value.map((item) => item.state)).toEqual([
      'uploading',
      'uploading',
      'queued',
    ])

    releases.shift()!()
    await flushPromises()

    expect(api.upload).toHaveBeenCalledTimes(3)
    expect(onUploaded).toHaveBeenCalledWith(expect.objectContaining({ id: 'a.pdf' }), 'kb-1')
    expect(upload.items.value[0]).toMatchObject({ state: 'done', progress: 100 })
  })

  it('shows the translated reason of a refused upload', async () => {
    api.upload.mockResolvedValueOnce({
      success: false,
      data: null,
      notifications: [{ key: 'document.duplicate', message: 'Duplicate.', type: 'conflict' }],
      status: 409,
    })
    const { upload } = await uploader()

    upload.add([pdf('manual.pdf')])
    await flushPromises()

    expect(upload.items.value[0]).toMatchObject({
      state: 'failed',
      error: 'Este arquivo já existe na base.',
    })
  })

  it('drops a file that is still waiting when it is cancelled', async () => {
    api.upload.mockImplementation(() => new Promise(() => undefined))
    const { upload } = await uploader()
    upload.add([pdf('a.pdf'), pdf('b.pdf'), pdf('c.pdf')])

    upload.cancel(upload.items.value[2]!.key)
    await flushPromises()

    expect(upload.items.value.map((item) => item.name)).toEqual(['a.pdf', 'b.pdf'])
    expect(api.upload).toHaveBeenCalledTimes(2)
  })
})
