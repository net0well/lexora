import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, shallowRef, type ShallowRef } from 'vue'

import type { ApiResult } from '@/api/types/commandResult'
import type { DocumentDetails, DocumentStatus } from '@/api/types/document'
import { ok } from '@/__tests__/tokens'
import { useDocumentStatusPolling } from '../useDocumentStatusPolling'

function details(status: DocumentStatus, embedded = 0): DocumentDetails {
  return {
    id: 'doc-1',
    knowledgeBaseId: 'kb-1',
    fileName: 'manual.pdf',
    format: 'pdf',
    contentType: 'application/pdf',
    sizeBytes: 10,
    status,
    processingVersion: 1,
    pageCount: 3,
    chunkCount: 4,
    embeddedChunkCount: embedded,
    failureReason: null,
    uploadedBy: 'user-1',
    uploadedAt: '2026-11-20T10:00:00Z',
    indexedAt: null,
    updatedAt: '2026-11-20T10:00:00Z',
  }
}

function setVisibility(state: DocumentVisibilityState): void {
  Object.defineProperty(document, 'visibilityState', { configurable: true, value: state })
  document.dispatchEvent(new Event('visibilitychange'))
}

function host(
  ids: ShallowRef<string[]>,
  fetch: (id: string, signal: AbortSignal) => Promise<ApiResult<DocumentDetails>>,
  onUpdate = vi.fn(),
) {
  const wrapper = mount(
    defineComponent({
      setup() {
        useDocumentStatusPolling({ ids, fetch, onUpdate })
        return () => null
      },
    }),
  )

  return { wrapper, onUpdate }
}

describe('useDocumentStatusPolling', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    setVisibility('visible')
  })

  it('asks for the status every 3 seconds and reports each answer', async () => {
    const fetch = vi.fn(async () => ok(details('extracted')))
    const { onUpdate } = host(shallowRef(['doc-1']), fetch)

    await vi.advanceTimersByTimeAsync(2_999)
    expect(fetch).not.toHaveBeenCalled()

    await vi.advanceTimersByTimeAsync(1)
    expect(fetch).toHaveBeenCalledWith('doc-1', expect.any(AbortSignal))
    expect(onUpdate).toHaveBeenCalledWith(expect.objectContaining({ status: 'extracted' }))
  })

  it('waits longer while nothing changes, up to 15 seconds', async () => {
    const fetch = vi.fn(async () => ok(details('chunked', 1)))
    host(shallowRef(['doc-1']), fetch)

    await vi.advanceTimersByTimeAsync(3_000)
    await vi.advanceTimersByTimeAsync(3_000)
    await vi.advanceTimersByTimeAsync(6_000)
    await vi.advanceTimersByTimeAsync(12_000)
    await vi.advanceTimersByTimeAsync(15_000)

    expect(fetch).toHaveBeenCalledTimes(5)
  })

  it('goes back to the short interval when the processing moves', async () => {
    let embedded = 0
    const fetch = vi.fn(async () => ok(details('chunked', embedded++)))
    host(shallowRef(['doc-1']), fetch)

    await vi.advanceTimersByTimeAsync(3_000 * 4)

    expect(fetch).toHaveBeenCalledTimes(4)
  })

  it('stops when no document is left to follow', async () => {
    const ids = shallowRef(['doc-1'])
    const fetch = vi.fn(async () => ok(details('indexed')))
    host(
      ids,
      fetch,
      vi.fn(() => (ids.value = [])),
    )

    await vi.advanceTimersByTimeAsync(3_000)
    await vi.advanceTimersByTimeAsync(30_000)

    expect(fetch).toHaveBeenCalledTimes(1)
  })

  it('pauses while the tab is hidden and polls again when it comes back', async () => {
    const fetch = vi.fn(async () => ok(details('extracted')))
    host(shallowRef(['doc-1']), fetch)

    setVisibility('hidden')
    await vi.advanceTimersByTimeAsync(30_000)
    expect(fetch).not.toHaveBeenCalled()

    setVisibility('visible')
    await vi.advanceTimersByTimeAsync(0)
    expect(fetch).toHaveBeenCalledTimes(1)
  })

  it('cancels the request and the timer when its owner goes away', async () => {
    let signal: AbortSignal | undefined
    const fetch = vi.fn((_id: string, received: AbortSignal) => {
      signal = received
      return new Promise<ApiResult<DocumentDetails>>(() => undefined)
    })
    const { wrapper } = host(shallowRef(['doc-1']), fetch)
    await vi.advanceTimersByTimeAsync(3_000)

    wrapper.unmount()
    await vi.advanceTimersByTimeAsync(60_000)

    expect(signal?.aborted).toBe(true)
    expect(fetch).toHaveBeenCalledTimes(1)
  })
})
