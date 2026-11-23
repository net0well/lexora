import { onScopeDispose, toValue, watch, type MaybeRefOrGetter } from 'vue'

import { isAbort } from '@/api/http'
import type { ApiResult } from '@/api/types/commandResult'
import type { DocumentDetails } from '@/api/types/document'

export interface UseDocumentStatusPollingOptions {
  ids: MaybeRefOrGetter<readonly string[]>
  fetch: (id: string, signal: AbortSignal) => Promise<ApiResult<DocumentDetails>>
  onUpdate: (document: DocumentDetails) => void
  intervalMs?: number
  maxIntervalMs?: number
}

function progressOf(document: DocumentDetails): string {
  return `${document.status}:${document.chunkCount}:${document.embeddedChunkCount}`
}

export function useDocumentStatusPolling({
  ids,
  fetch,
  onUpdate,
  intervalMs = 3_000,
  maxIntervalMs = 15_000,
}: UseDocumentStatusPollingOptions): void {
  const lastSeen = new Map<string, string>()
  let delay = intervalMs
  let timer: ReturnType<typeof setTimeout> | undefined
  let controller: AbortController | null = null

  function hidden(): boolean {
    return document.visibilityState === 'hidden'
  }

  function stop(): void {
    clearTimeout(timer)
    timer = undefined
    controller?.abort()
    controller = null
  }

  function schedule(): void {
    clearTimeout(timer)

    if (!hidden() && toValue(ids).length > 0) {
      timer = setTimeout(() => void poll(), delay)
    }
  }

  async function poll(): Promise<void> {
    const followed = [...toValue(ids)]

    if (followed.length === 0 || hidden()) {
      return
    }

    controller = new AbortController()
    const signal = controller.signal
    let progressed = false

    try {
      const results = await Promise.all(followed.map((id) => fetch(id, signal)))

      for (const result of results) {
        if (result.success && result.data) {
          const progress = progressOf(result.data)
          progressed ||= lastSeen.get(result.data.id) !== progress
          lastSeen.set(result.data.id, progress)
          onUpdate(result.data)
        }
      }
    } catch (error) {
      if (isAbort(error)) {
        return
      }

      throw error
    } finally {
      if (controller?.signal === signal) {
        controller = null
      }
    }

    delay = progressed ? intervalMs : Math.min(delay * 2, maxIntervalMs)
    schedule()
  }

  function visibilityChanged(): void {
    stop()

    if (!hidden()) {
      delay = intervalMs
      void poll()
    }
  }

  watch(
    () => [...toValue(ids)].sort().join(','),
    () => {
      stop()
      delay = intervalMs
      schedule()
    },
    { immediate: true },
  )

  document.addEventListener('visibilitychange', visibilityChanged)

  onScopeDispose(() => {
    stop()
    document.removeEventListener('visibilitychange', visibilityChanged)
  })
}
